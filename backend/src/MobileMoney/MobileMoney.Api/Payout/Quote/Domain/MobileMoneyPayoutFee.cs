namespace MobileMoney.Production.Payout.Quote.Domain;

public sealed record MobileMoneyPayoutFee
{
    public MobileMoneyPayoutFee(string code, long amountMinor, string currency)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Fee code is required.", nameof(code));
        if (amountMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        Code = code.Trim().ToUpperInvariant();
        AmountMinor = amountMinor;
        Currency = NormalizeCurrency(currency);
    }

    public string Code { get; }
    public long AmountMinor { get; }
    public string Currency { get; }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException(
                "Currency must contain three ISO-like letters.",
                nameof(value));

        return normalized;
    }
}
