using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestActionStatus
{
    Success = 1,
    NotFound = 2,
    ActorNotAllowed = 3,
    RecipientNotFound = 4
}

public sealed record PaymentRequestActionResult(
    PaymentRequestActionStatus Status,
    PaymentRequestSnapshot? Request)
{
    public static PaymentRequestActionResult Succeeded(PaymentRequest request) =>
        new(PaymentRequestActionStatus.Success, PaymentRequestMappings.ToSnapshot(request));

    public static PaymentRequestActionResult NotFound() =>
        new(PaymentRequestActionStatus.NotFound, null);

    public static PaymentRequestActionResult ActorNotAllowed() =>
        new(PaymentRequestActionStatus.ActorNotAllowed, null);

    public static PaymentRequestActionResult RecipientNotFound() =>
        new(PaymentRequestActionStatus.RecipientNotFound, null);
}

public sealed record PaymentRequestPaymentReceipt(
    Guid TransferId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc);
