using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed class MobileMoneyPayoutFundingPlanningService
{
    private readonly IMobileMoneyPayoutFundingSourceReader _sourceReader;
    private readonly IMobileMoneyPayoutFundingAttemptStore _attemptStore;
    private readonly MobileMoneyPayoutSplitFundingValidator _validator;

    public MobileMoneyPayoutFundingPlanningService(
        IMobileMoneyPayoutFundingSourceReader sourceReader,
        IMobileMoneyPayoutFundingAttemptStore attemptStore,
        MobileMoneyPayoutSplitFundingValidator validator)
    {
        _sourceReader = sourceReader ??
            throw new ArgumentNullException(nameof(sourceReader));
        _attemptStore = attemptStore ??
            throw new ArgumentNullException(nameof(attemptStore));
        _validator = validator ??
            throw new ArgumentNullException(nameof(validator));
    }

    public async Task<MobileMoneyPayoutFundingPlan> PlanAsync(
        PlanMobileMoneyPayoutFundingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Allocations is null)
        {
            throw new ArgumentException(
                "Funding allocations are required.",
                nameof(command));
        }

        ValidateReplayIdentity(command);

        var existing = await _attemptStore.FindByCorrelationIdAsync(
            command.CorrelationId,
            cancellationToken);

        if (existing.Count > 0)
        {
            return BuildAuthoritativePlan(command, existing);
        }

        var sources =
            new List<FundingSourceSnapshot>(command.Allocations.Count);

        foreach (var allocation in command.Allocations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(allocation);

            var source = await _sourceReader.GetAsync(
                allocation.SourceId,
                allocation.SourceType,
                cancellationToken);

            if (source is not null)
            {
                sources.Add(source);
            }
        }

        var candidate = _validator.ValidateAndPlan(command, sources);
        var attempts = candidate.Allocations
            .Select(allocation => FundingAttempt.Create(
                Guid.NewGuid(),
                candidate.CorrelationId,
                allocation,
                candidate.PlannedAtUtc))
            .ToArray();

        var authoritative = await _attemptStore.SavePlanAsync(
            attempts,
            cancellationToken);

        return BuildAuthoritativePlan(command, authoritative);
    }

    private static void ValidateReplayIdentity(
        PlanMobileMoneyPayoutFundingCommand command)
    {
        if (command.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation id cannot be empty.",
                nameof(command));
        }

        if (command.RequiredAmountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Required funding amount must be positive.");
        }

        if (command.RequestedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Funding request timestamp must be UTC.",
                nameof(command));
        }

        _ = FundingAllocation.NormalizeCurrency(command.CurrencyCode);

        if (command.Allocations.Count == 0)
        {
            throw new ArgumentException(
                "At least one funding allocation is required.",
                nameof(command));
        }
    }

    private static MobileMoneyPayoutFundingPlan BuildAuthoritativePlan(
        PlanMobileMoneyPayoutFundingCommand command,
        IReadOnlyList<FundingAttempt> attempts)
    {
        if (attempts.Count == 0)
        {
            throw new InvalidOperationException(
                "Funding persistence returned an empty authoritative plan.");
        }

        var plannedAtUtc = attempts[0].CreatedAtUtc;

        if (attempts.Any(attempt =>
                attempt.CorrelationId != command.CorrelationId
                || attempt.Status != FundingAttemptStatus.Planned
                || attempt.CreatedAtUtc != plannedAtUtc
                || attempt.UpdatedAtUtc != plannedAtUtc
                || attempt.StatusReason is not null))
        {
            throw new InvalidOperationException(
                "Persisted funding attempts do not represent one complete planned funding plan.");
        }

        var expectedCurrency =
            FundingAllocation.NormalizeCurrency(command.CurrencyCode);

        if (attempts.Count != command.Allocations.Count)
        {
            throw new InvalidOperationException(
                "Funding correlation is already bound to a different allocation set.");
        }

        var requestedBySource = new Dictionary<string, FundingAllocation>(
            StringComparer.Ordinal);

        foreach (var allocation in command.Allocations)
        {
            ArgumentNullException.ThrowIfNull(allocation);

            if (!requestedBySource.TryAdd(allocation.SourceId, allocation))
            {
                throw new InvalidOperationException(
                    "Funding request contains duplicate source identifiers.");
            }
        }

        long persistedTotalMinor = 0;
        foreach (var attempt in attempts)
        {
            var allocation = attempt.Allocation;

            if (!string.Equals(
                    allocation.CurrencyCode,
                    expectedCurrency,
                    StringComparison.Ordinal)
                || !requestedBySource.TryGetValue(
                    allocation.SourceId,
                    out var requested)
                || requested.SourceType != allocation.SourceType
                || requested.AmountMinor != allocation.AmountMinor
                || !string.Equals(
                    requested.CurrencyCode,
                    allocation.CurrencyCode,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Funding correlation is already bound to different funding material.");
            }

            persistedTotalMinor =
                checked(persistedTotalMinor + allocation.AmountMinor);
        }

        if (persistedTotalMinor != command.RequiredAmountMinor)
        {
            throw new InvalidOperationException(
                "Funding correlation is already bound to a different required amount.");
        }

        return new MobileMoneyPayoutFundingPlan(
            command.CorrelationId,
            command.RequiredAmountMinor,
            expectedCurrency,
            attempts.Select(attempt => attempt.Allocation).ToArray(),
            plannedAtUtc);
    }
}
