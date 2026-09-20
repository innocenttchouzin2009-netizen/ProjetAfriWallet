using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestRecoveryStatus
{
    Reconciled = 1,
    AlreadyPaid = 2,
    PaymentNotFound = 3,
    RequestNotFound = 4,
    NotEligible = 5
}

public sealed record PaymentRequestRecoveryResult(
    PaymentRequestRecoveryStatus Status,
    PaymentRequestSnapshot? Request)
{
    public static PaymentRequestRecoveryResult Reconciled(PaymentRequest request) =>
        new(PaymentRequestRecoveryStatus.Reconciled, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestRecoveryResult AlreadyPaid(PaymentRequest request) =>
        new(PaymentRequestRecoveryStatus.AlreadyPaid, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestRecoveryResult PaymentNotFound(PaymentRequest request) =>
        new(PaymentRequestRecoveryStatus.PaymentNotFound, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestRecoveryResult RequestNotFound() =>
        new(PaymentRequestRecoveryStatus.RequestNotFound, null);

    public static PaymentRequestRecoveryResult NotEligible(PaymentRequest request) =>
        new(PaymentRequestRecoveryStatus.NotEligible, PaymentRequestMappings.ToSnapshot(request));
}
