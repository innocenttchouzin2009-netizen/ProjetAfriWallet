namespace BankRouting.Domain.Routing;

public sealed record RoutingRequest(
    Guid TransferIntentId,
    string OwnerAwid,
    string CountryCode,
    string CurrencyCode,
    long AmountMinor,
    string IdempotencyKey,
    string? PreferredRail = null)
{
    public void EnsureValid()
    {
        if (TransferIntentId == Guid.Empty)
            throw new ArgumentException("Transfer intent ID is required.", nameof(TransferIntentId));

        if (string.IsNullOrWhiteSpace(OwnerAwid))
            throw new ArgumentException("Owner AWID is required.", nameof(OwnerAwid));

        if (string.IsNullOrWhiteSpace(CountryCode))
            throw new ArgumentException("Country code is required.", nameof(CountryCode));

        if (string.IsNullOrWhiteSpace(CurrencyCode))
            throw new ArgumentException("Currency code is required.", nameof(CurrencyCode));

        if (AmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(AmountMinor), "Amount must be positive.");

        if (string.IsNullOrWhiteSpace(IdempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(IdempotencyKey));
    }
}
