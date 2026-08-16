namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

public sealed record MonitoredTransaction(
    Guid TransactionId,
    string Awid,
    TransactionDirection Direction,
    TransactionChannel Channel,
    long AmountMinor,
    string CurrencyCode,
    string CountryCode,
    string? CounterpartyId,
    string? BeneficiaryId,
    DateTimeOffset OccurredAtUtc)
{
    public MonitoredTransaction Normalize()
    {
        if (TransactionId == Guid.Empty)
            throw new ArgumentException("Transaction ID is required.");
        if (string.IsNullOrWhiteSpace(Awid))
            throw new ArgumentException("AWID is required.");
        if (AmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(AmountMinor));
        if (string.IsNullOrWhiteSpace(CurrencyCode))
            throw new ArgumentException("Currency is required.", nameof(CurrencyCode));
        if (string.IsNullOrWhiteSpace(CountryCode))
            throw new ArgumentException("Country is required.", nameof(CountryCode));

        var currency = CurrencyCode.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new ArgumentException("Currency must use ISO 4217.");

        var country = CountryCode.Trim().ToUpperInvariant();
        if (country.Length != 2)
            throw new ArgumentException("Country must use ISO alpha-2.");

        return this with
        {
            Awid = Awid.Trim(),
            CurrencyCode = currency,
            CountryCode = country,
            CounterpartyId = CounterpartyId?.Trim(),
            BeneficiaryId = BeneficiaryId?.Trim()
        };
    }
}