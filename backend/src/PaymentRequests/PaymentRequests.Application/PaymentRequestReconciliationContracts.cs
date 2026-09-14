using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestPaymentReceipt(
    Guid TransferId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc);

public enum PaymentRequestReconciliationStatus
{
    Reconciled = 1,
    AlreadyPaid = 2,
    TransferNotFound = 3,
    NotEligible = 4,
    RequestNotFound = 5
}

public sealed record PaymentRequestReconciliationResult(
    PaymentRequestReconciliationStatus Status,
    PaymentRequestSnapshot? Request)
{
    public static PaymentRequestReconciliationResult Reconciled(PaymentRequest request) =>
        new(PaymentRequestReconciliationStatus.Reconciled, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestReconciliationResult AlreadyPaid(PaymentRequest request) =>
        new(PaymentRequestReconciliationStatus.AlreadyPaid, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestReconciliationResult TransferNotFound(PaymentRequest request) =>
        new(PaymentRequestReconciliationStatus.TransferNotFound, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestReconciliationResult NotEligible(PaymentRequest request) =>
        new(PaymentRequestReconciliationStatus.NotEligible, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestReconciliationResult RequestNotFound() =>
        new(PaymentRequestReconciliationStatus.RequestNotFound, null);
}
