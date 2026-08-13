namespace MerchantSettlement.Domain.Reconciliation;

public sealed record MerchantReconciliationResult(
    Guid SettlementId,
    string MerchantId,
    MerchantReconciliationStatus Status,
    long ExpectedMinor,
    long ActualMinor,
    long DifferenceMinor,
    string? ExceptionCode,
    DateTime CheckedAtUtc);

public enum MerchantReconciliationStatus
{
    Matched,
    Difference,
    Missing,
    Pending
}
