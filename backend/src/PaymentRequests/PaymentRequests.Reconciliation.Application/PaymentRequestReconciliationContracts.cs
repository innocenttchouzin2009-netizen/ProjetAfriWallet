using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Reconciliation.Application;

public sealed record TransferReceiptSnapshot(
    Guid TransferId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc);

public enum PaymentRequestReconciliationStatus
{
    Reconciled = 1,
    AlreadyPaid = 2,
    NotFound = 3,
    NotEligible = 4,
    TransferReceiptNotFound = 5
}

public sealed record PaymentRequestReconciliationRecord(
    PaymentRequestId RequestId,
    PaymentRequestReconciliationStatus Status,
    Guid? TransferId);

public sealed record PaymentRequestReconciliationResult(
    PaymentRequestReconciliationStatus Status,
    PaymentRequest? Request)
{
    public static PaymentRequestReconciliationResult Reconciled(PaymentRequest request) => new(PaymentRequestReconciliationStatus.Reconciled, request);
    public static PaymentRequestReconciliationResult AlreadyPaid(PaymentRequest request) => new(PaymentRequestReconciliationStatus.AlreadyPaid, request);
    public static PaymentRequestReconciliationResult NotFound() => new(PaymentRequestReconciliationStatus.NotFound, null);
    public static PaymentRequestReconciliationResult NotEligible(PaymentRequest request) => new(PaymentRequestReconciliationStatus.NotEligible, request);
    public static PaymentRequestReconciliationResult TransferReceiptNotFound(PaymentRequest request) => new(PaymentRequestReconciliationStatus.TransferReceiptNotFound, request);
}
