namespace Reconciliation.Domain.Records;

public sealed record InternalFinancialRecord(
    string RecordId,
    string Reference,
    string PartnerId,
    string CurrencyCode,
    long AmountMinor,
    DateTime OccurredAtUtc,
    string Source);
