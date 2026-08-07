using Settlement.Application.Interfaces;

namespace Settlement.Infrastructure.Gateways;

public sealed class SandboxTreasurySettlementGateway : ITreasurySettlementGateway
{
    private readonly Dictionary<string, long> _availableByAccountCurrency = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:XAF"] = 10_000_000,
        ["bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb:USD"] = 25_000,
        ["cccccccc-cccc-cccc-cccc-cccccccccccc:EUR"] = 20_000
    };

    private readonly HashSet<Guid> _postedInstructions = [];

    public Task<bool> HasAvailableFundsAsync(
        Guid accountId,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = ToKey(accountId, currencyCode);
        var available = _availableByAccountCurrency.TryGetValue(key, out var value) ? value : 0;
        return Task.FromResult(available >= amountMinor);
    }

    public Task PostSettlementAsync(TreasurySettlementPosting posting, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_postedInstructions.Contains(posting.InstructionId))
        {
            return Task.CompletedTask;
        }

        var sourceKey = ToKey(posting.SourceAccountId, posting.SourceCurrency);
        var destinationKey = ToKey(posting.DestinationAccountId, posting.DestinationCurrency);

        var sourceAvailable = _availableByAccountCurrency.TryGetValue(sourceKey, out var src)
            ? src
            : 0;

        if (sourceAvailable < posting.SourceAmountMinor)
        {
            throw new InvalidOperationException("Treasury posting rejected: insufficient source funds.");
        }

        _availableByAccountCurrency[sourceKey] = sourceAvailable - posting.SourceAmountMinor;

        var destinationAvailable = _availableByAccountCurrency.TryGetValue(destinationKey, out var dst)
            ? dst
            : 0;

        _availableByAccountCurrency[destinationKey] = destinationAvailable + posting.DestinationAmountMinor;
        _postedInstructions.Add(posting.InstructionId);

        return Task.CompletedTask;
    }

    private static string ToKey(Guid accountId, string currencyCode)
    {
        return $"{accountId:D}:{currencyCode.Trim().ToUpperInvariant()}";
    }
}
