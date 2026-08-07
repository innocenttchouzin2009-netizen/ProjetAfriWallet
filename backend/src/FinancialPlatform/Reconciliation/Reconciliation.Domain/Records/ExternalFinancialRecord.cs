namespace Reconciliation.Domain.Records;

public sealed record ExternalFinancialRecord(
    string RecordId,
    string ExternalReference,
    string PartnerId,
    string CurrencyCode,
    long AmountMinor,
    DateTime OccurredAtUtc,
    string Source);
