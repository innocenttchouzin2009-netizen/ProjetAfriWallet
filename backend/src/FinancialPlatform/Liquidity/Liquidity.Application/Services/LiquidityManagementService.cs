using Liquidity.Application.Interfaces;
using Liquidity.Domain.Forecast;
using Liquidity.Domain.Positions;
using Liquidity.Domain.Rebalancing;
using Liquidity.Domain.Snapshots;
using Liquidity.Domain.Thresholds;

namespace Liquidity.Application.Services;

public sealed class LiquidityManagementService
{
    private readonly ITreasuryReadModel _treasuryReadModel;
    private readonly ILiquiditySnapshotRepository _snapshotRepository;

    public LiquidityManagementService(
        ITreasuryReadModel treasuryReadModel,
        ILiquiditySnapshotRepository snapshotRepository)
    {
        _treasuryReadModel = treasuryReadModel;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<IReadOnlyCollection<LiquidityPosition>> GetPositionsAsync(
        CancellationToken cancellationToken)
    {
        var states = await _treasuryReadModel.GetAccountStatesAsync(cancellationToken);

        return states
            .Select(ToPosition)
            .OrderBy(x => x.CurrencyCode)
            .ThenBy(x => x.AccountId)
            .ToArray();
    }

    public async Task<LiquidityPosition> GetAccountPositionAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var states = await _treasuryReadModel.GetAccountStatesAsync(cancellationToken);
        var state = states.FirstOrDefault(x => x.AccountId == accountId)
            ?? throw new KeyNotFoundException("Liquidity account not found.");

        return ToPosition(state);
    }

    public async Task<IReadOnlyCollection<LiquidityAlert>> GetAlertsAsync(
        LiquidityThreshold threshold,
        CancellationToken cancellationToken)
    {
        var positions = await GetPositionsAsync(cancellationToken);

        return positions
            .Select(x => EvaluateAlert(x, threshold))
            .ToArray();
    }

    public async Task<LiquidityForecast> GetForecastAsync(
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var positions = await GetPositionsAsync(cancellationToken);
        var currency = currencyCode.Trim().ToUpperInvariant();

        var net = positions
            .Where(x => string.Equals(x.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.NetMinor);

        // Conservative, deterministic forecast model until predictive signals are introduced.
        var j1 = net;
        var j7 = net - (net / 20);
        var j30 = net - (net / 10);

        return new LiquidityForecast(currency, j1, j7, j30, DateTime.UtcNow);
    }

    public async Task<LiquiditySnapshot> CreateSnapshotAsync(
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var positions = await GetPositionsAsync(cancellationToken);
        var currency = currencyCode.Trim().ToUpperInvariant();

        var filtered = positions
            .Where(x => string.Equals(x.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var snapshot = new LiquiditySnapshot(
            Guid.NewGuid(),
            DateTime.UtcNow,
            currency,
            filtered.Sum(x => x.AvailableMinor),
            filtered.Sum(x => x.ReservedMinor),
            filtered.Sum(x => x.PendingMinor),
            filtered.Sum(x => x.BlockedMinor),
            filtered.Sum(x => x.NetMinor));

        await _snapshotRepository.SaveAsync(snapshot, cancellationToken);

        return snapshot;
    }

    public Task<IReadOnlyCollection<LiquiditySnapshot>> GetSnapshotsAsync(
        CancellationToken cancellationToken)
        => _snapshotRepository.GetAllAsync(cancellationToken);

    public async Task<LiquidityRebalance> ProposeRebalanceAsync(
        string currencyCode,
        LiquidityThreshold threshold,
        CancellationToken cancellationToken)
    {
        var positions = await GetPositionsAsync(cancellationToken);
        var currency = currencyCode.Trim().ToUpperInvariant();

        var sameCurrency = positions
            .Where(x => string.Equals(x.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var deficits = sameCurrency
            .Where(x => x.AvailableMinor < threshold.MinimumMinor)
            .OrderBy(x => x.AvailableMinor)
            .ToList();

        var surpluses = sameCurrency
            .Where(x => x.AvailableMinor > threshold.WarningMinor)
            .OrderByDescending(x => x.AvailableMinor)
            .ToList();

        var transfers = new List<LiquidityTransfer>();

        foreach (var deficit in deficits)
        {
            var needed = threshold.MinimumMinor - deficit.AvailableMinor;
            if (needed <= 0)
            {
                continue;
            }

            for (var i = 0; i < surpluses.Count && needed > 0; i++)
            {
                var source = surpluses[i];
                var transferable = source.AvailableMinor - threshold.WarningMinor;
                if (transferable <= 0)
                {
                    continue;
                }

                var amount = Math.Min(needed, transferable);
                transfers.Add(new LiquidityTransfer(source.AccountId, deficit.AccountId, amount, currency));

                needed -= amount;
                surpluses[i] = source with { AvailableMinor = source.AvailableMinor - amount };
            }
        }

        return new LiquidityRebalance(
            currency,
            DateTime.UtcNow,
            transfers,
            transfers.Count == 0
                ? "No rebalance required."
                : "Rebalance proposed for deficit coverage.");
    }

    private static LiquidityPosition ToPosition(TreasuryLiquidityAccountState state)
    {
        var available = state.LedgerNetMinor - state.ReservedMinor - state.PendingMinor - state.BlockedMinor;

        return new LiquidityPosition(
            state.AccountId,
            state.CurrencyCode,
            available,
            state.ReservedMinor,
            state.PendingMinor,
            state.BlockedMinor,
            state.LedgerNetMinor,
            DateTime.UtcNow);
    }

    private static LiquidityAlert EvaluateAlert(LiquidityPosition position, LiquidityThreshold threshold)
    {
        if (position.AvailableMinor <= threshold.CriticalMinor)
        {
            return new LiquidityAlert(
                position.AccountId,
                position.CurrencyCode,
                LiquidityAlertLevel.Critical,
                position.NetMinor,
                position.AvailableMinor,
                DateTime.UtcNow,
                "Available liquidity is at or below critical threshold.");
        }

        if (position.AvailableMinor <= threshold.WarningMinor)
        {
            return new LiquidityAlert(
                position.AccountId,
                position.CurrencyCode,
                LiquidityAlertLevel.Warning,
                position.NetMinor,
                position.AvailableMinor,
                DateTime.UtcNow,
                "Available liquidity is at or below warning threshold.");
        }

        return new LiquidityAlert(
            position.AccountId,
            position.CurrencyCode,
            LiquidityAlertLevel.Healthy,
            position.NetMinor,
            position.AvailableMinor,
            DateTime.UtcNow,
            "Liquidity is healthy.");
    }
}
