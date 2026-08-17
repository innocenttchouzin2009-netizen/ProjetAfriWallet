namespace AfriWallet.Fraud.TransactionFraud.Api.Contracts;

public sealed record DetectTransactionFraudRequest(
    Guid TransactionId,
    string Awid,
    string DeviceId,
    string BeneficiaryId,
    long AmountMinor,
    string CurrencyCode,
    string CountryCode,
    DateTimeOffset OccurredAtUtc);
