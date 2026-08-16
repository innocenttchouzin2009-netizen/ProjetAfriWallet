namespace AfriWallet.Fraud.TransactionFraud.Domain.Transactions;

public sealed record FraudTransaction(
    Guid TransactionId,
    string Awid,
    string DeviceId,
    string BeneficiaryId,
    long AmountMinor,
    string CurrencyCode,
    string CountryCode,
    DateTimeOffset OccurredAtUtc)
{
    public FraudTransaction Normalize()
    {
        if (TransactionId == Guid.Empty)
            throw new ArgumentException("Transaction ID is required.");
        if (string.IsNullOrWhiteSpace(Awid))
            throw new ArgumentException("AWID is required.");
        if (string.IsNullOrWhiteSpace(DeviceId))
            throw new ArgumentException("Device ID is required.");
        if (string.IsNullOrWhiteSpace(BeneficiaryId))
            throw new ArgumentException("Beneficiary ID is required.");
        if (AmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(AmountMinor));

        var currency = CurrencyCode.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new ArgumentException("Currency must use ISO 4217.");

        var country = CountryCode.Trim().ToUpperInvariant();
        if (country.Length != 2)
            throw new ArgumentException("Country code must be ISO alpha-2.");

        return this with
        {
            Awid = Awid.Trim(),
            DeviceId = DeviceId.Trim(),
            BeneficiaryId = BeneficiaryId.Trim(),
            CurrencyCode = currency,
            CountryCode = country
        };
    }
}
