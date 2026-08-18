namespace AfriWallet.Disputes.Registry.Domain.Claims;

public sealed record DisputeClaimAmount
{
    public DisputeClaimAmount(long amountMinor, string currency)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Claim amount must be positive.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));

        AmountMinor = amountMinor;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public long AmountMinor { get; }
    public string Currency { get; }
}
