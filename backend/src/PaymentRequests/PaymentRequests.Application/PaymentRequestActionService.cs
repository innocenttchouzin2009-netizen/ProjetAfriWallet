using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestActionService(
    IPaymentRequestRepository repository,
    IPaymentRequestRecipientResolver recipientResolver,
    IPaymentRequestWalletOwnershipReader walletOwnershipReader,
    IPaymentRequestPaymentPort paymentPort)
{
    public async Task<PaymentRequestActionResult> DeclineAsync(
        PaymentRequestId id,
        Guid actorUserId,
        DateTimeOffset actionAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actorUserId, actionAtUtc);
        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null)
        {
            return PaymentRequestActionResult.NotFound();
        }

        var payerWalletId = await recipientResolver.ResolveAsync(
            request.PayerReference,
            request.Currency,
            cancellationToken);
        if (payerWalletId is null)
        {
            return PaymentRequestActionResult.RecipientNotFound();
        }

        if (!await walletOwnershipReader.IsOwnedByAsync(payerWalletId.Value, actorUserId, cancellationToken))
        {
            return PaymentRequestActionResult.ActorNotAllowed();
        }

        request.Decline(actionAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return PaymentRequestActionResult.Succeeded(request);
    }

    public async Task<PaymentRequestActionResult> CancelAsync(
        PaymentRequestId id,
        Guid actorUserId,
        DateTimeOffset actionAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actorUserId, actionAtUtc);
        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null)
        {
            return PaymentRequestActionResult.NotFound();
        }

        if (!await walletOwnershipReader.IsOwnedByAsync(request.RequesterWalletId, actorUserId, cancellationToken))
        {
            return PaymentRequestActionResult.ActorNotAllowed();
        }

        request.Cancel(actionAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return PaymentRequestActionResult.Succeeded(request);
    }

    public async Task<PaymentRequestActionResult> AcceptAndPayAsync(
        PaymentRequestId id,
        Guid actorUserId,
        DateTimeOffset actionAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actorUserId, actionAtUtc);
        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null)
        {
            return PaymentRequestActionResult.NotFound();
        }

        var payerWalletId = await recipientResolver.ResolveAsync(
            request.PayerReference,
            request.Currency,
            cancellationToken);
        if (payerWalletId is null)
        {
            return PaymentRequestActionResult.RecipientNotFound();
        }

        if (!await walletOwnershipReader.IsOwnedByAsync(payerWalletId.Value, actorUserId, cancellationToken))
        {
            return PaymentRequestActionResult.ActorNotAllowed();
        }

        request.Accept(payerWalletId.Value, actionAtUtc);

        // The payment correlation is deterministically bound to the request id so a retry
        // cannot create a second ledger journal through the certified transfer engine.
        var payment = await paymentPort.ExecuteAsync(
            payerWalletId.Value.Value,
            request.RequesterWalletId.Value,
            request.AmountMinor,
            request.Id.Value,
            actionAtUtc,
            cancellationToken);

        if (payment.SourceWalletId != payerWalletId.Value.Value ||
            payment.TargetWalletId != request.RequesterWalletId.Value ||
            payment.AmountMinor != request.AmountMinor ||
            payment.CorrelationId != request.Id.Value)
        {
            throw new InvalidOperationException("Payment execution receipt does not match the payment request.");
        }

        request.MarkPaid(payment.TransferId, payment.CreatedAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return PaymentRequestActionResult.Succeeded(request);
    }

    private static void ValidateActor(Guid actorUserId, DateTimeOffset actionAtUtc)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user id cannot be empty.", nameof(actorUserId));
        }

        if (actionAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Action timestamp must be UTC.", nameof(actionAtUtc));
        }
    }
}
