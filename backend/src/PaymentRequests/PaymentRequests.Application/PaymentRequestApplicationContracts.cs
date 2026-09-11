using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed record CreatePaymentRequestCommand(
    WalletId RequesterWalletId,
    RecipientReference PayerReference,
    Currency Currency,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc = null);

public enum CreatePaymentRequestStatus
{
    Created = 1,
    Existing = 2,
    RecipientNotFound = 3,
    SelfRequestNotAllowed = 4
}

public sealed record PaymentRequestSnapshot(
    PaymentRequestId Id,
    WalletId RequesterWalletId,
    RecipientReference PayerReference,
    Currency Currency,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    PaymentRequestStatus Status,
    WalletId? AcceptedPayerWalletId,
    DateTimeOffset? AcceptedAtUtc,
    Guid? TransferId,
    DateTimeOffset? ClosedAtUtc);

public sealed record CreatePaymentRequestResult(
    CreatePaymentRequestStatus Status,
    PaymentRequestSnapshot? Request)
{
    public static CreatePaymentRequestResult Created(PaymentRequest request) =>
        new(CreatePaymentRequestStatus.Created, PaymentRequestMappings.ToSnapshot(request));

    public static CreatePaymentRequestResult Existing(PaymentRequest request) =>
        new(CreatePaymentRequestStatus.Existing, PaymentRequestMappings.ToSnapshot(request));

    public static CreatePaymentRequestResult RecipientNotFound() =>
        new(CreatePaymentRequestStatus.RecipientNotFound, null);

    public static CreatePaymentRequestResult SelfRequestNotAllowed() =>
        new(CreatePaymentRequestStatus.SelfRequestNotAllowed, null);
}

internal static class PaymentRequestMappings
{
    public static PaymentRequestSnapshot ToSnapshot(PaymentRequest request) => new(
        request.Id,
        request.RequesterWalletId,
        request.PayerReference,
        request.Currency,
        request.AmountMinor,
        request.CorrelationId,
        request.CreatedAtUtc,
        request.ExpiresAtUtc,
        request.Status,
        request.AcceptedPayerWalletId,
        request.AcceptedAtUtc,
        request.TransferId,
        request.ClosedAtUtc);
}
