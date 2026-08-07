namespace Reconciliation.Contracts.Requests;

public sealed record StartReconciliationRequest(
    string PartnerId,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc);
