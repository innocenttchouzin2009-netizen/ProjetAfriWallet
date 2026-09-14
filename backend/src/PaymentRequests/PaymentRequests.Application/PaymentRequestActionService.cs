using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

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

        if (request.Status == PaymentRequestStatus.Paid)
        {
            var paidPayerWalletId = RequireAcceptedPayerWallet(request);
            return await walletOwnershipReader.IsOwnedByAsync(paidPayerWalletId, actorUserId, cancellationToken)
                ? PaymentRequestActionResult.Succeeded(request)
                : PaymentRequestActionResult.ActorNotAllowed();
        }

        WalletId payerWalletId;
        if (request.Status == PaymentRequestStatus.Accepted)
        {
            payerWalletId = RequireAcceptedPayerWallet(request);
        }
        else if (request.Status == PaymentRequestStatus.Pending)
        {
            var resolvedPayerWalletId = await recipientResolver.ResolveAsync(
                request.PayerReference,
                request.Currency,
                cancellationToken);
            if (resolvedPayerWalletId is null)
            {
                return PaymentRequestActionResult.RecipientNotFound();
            }

            payerWalletId = resolvedPayerWalletId.Value;
        }
        else
        {
            throw new InvalidOperationException($"Payment request in status {request.Status} cannot be accepted and paid.");
        }

        if (!await walletOwnershipReader.IsOwnedByAsync(payerWalletId, actorUserId, cancellationToken))
        {
            return PaymentRequestActionResult.ActorNotAllowed();
        }

        if (request.Status == PaymentRequestStatus.Pending)
        {
            request.Accept(payerWalletId, actionAtUtc);
            await repository.UpdateAsync(request, cancellationToken);
        }
        else
        {
            if (actionAtUtc < request.UpdatedAtUtc)
            {
                throw new ArgumentException("Action timestamp cannot move backwards.", nameof(actionAtUtc));
            }

            if (request.ExpiresAtUtc is not null && actionAtUtc >= request.ExpiresAtUtc.Value)
            {
                throw new InvalidOperationException("Payment request has expired.");
            }
        }

        // The payment correlation is deterministically bound to the request id so a retry
        // cannot create a second ledger journal through the certified transfer engine.
        var payment = await paymentPort.ExecuteAsync(
            payerWalletId.Value,
            request.RequesterWalletId.Value,
            request.AmountMinor,
            request.Id.Value,
            actionAtUtc,
            cancellationToken);

        if (payment.SourceWalletId != payerWalletId.Value ||
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

    private static WalletId RequireAcceptedPayerWallet(PaymentRequest request)
    {
        if (request.AcceptedPayerWalletId is null || request.AcceptedPayerWalletId.Value.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Accepted payment request is missing its payer wallet binding.");
        }

        return request.AcceptedPayerWalletId.Value;
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
