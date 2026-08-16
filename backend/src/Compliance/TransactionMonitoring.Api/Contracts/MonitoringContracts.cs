using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Api.Contracts;

public sealed record MonitorTransactionRequest(
    Guid TransactionId,
    string Awid,
    TransactionDirection Direction,
    TransactionChannel Channel,
    long AmountMinor,
    string CurrencyCode,
    string CountryCode,
    string? CounterpartyId,
    string? BeneficiaryId,
    DateTimeOffset OccurredAtUtc);