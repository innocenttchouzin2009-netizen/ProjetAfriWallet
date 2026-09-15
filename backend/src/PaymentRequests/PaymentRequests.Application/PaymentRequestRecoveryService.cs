using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestRecoveryService(
    IPaymentRequestRepository repository,
    IPaymentRequestReconciliationPort reconciliationPort)
{
    public async Task<PaymentRequestRecoveryResult> ReconcileAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null)
        {
            return PaymentRequestRecoveryResult.RequestNotFound();
        }

        if (request.Status == PaymentRequestStatus.Paid)
        {
            if (request.TransferId is null || request.TransferId == Guid.Empty)
            {
                throw new InvalidOperationException("Paid payment request is missing its transfer id.");
            }

            return PaymentRequestRecoveryResult.AlreadyPaid(request);
        }

        if (request.Status != PaymentRequestStatus.Accepted)
        {
            return PaymentRequestRecoveryResult.NotEligible(request);
        }

        if (request.AcceptedPayerWalletId is null || request.AcceptedAtUtc is null)
        {
            throw new InvalidOperationException("Accepted payment request is missing payer binding metadata.");
        }

        var receipt = await reconciliationPort.FindByCorrelationIdAsync(request.Id.Value, cancellationToken);
        if (receipt is null)
        {
            return PaymentRequestRecoveryResult.PaymentNotFound(request);
        }

        ValidateReceipt(request, receipt);

        request.MarkPaid(receipt.TransferId, receipt.CreatedAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return PaymentRequestRecoveryResult.Reconciled(request);
    }

    private static void ValidateReceipt(PaymentRequest request, PaymentRequestPaymentReceipt receipt)
    {
        if (receipt.TransferId == Guid.Empty)
        {
            throw new InvalidOperationException("Reconciliation receipt transfer id cannot be empty.");
        }

        if (receipt.CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Reconciliation receipt timestamp must be UTC.");
        }

        if (receipt.SourceWalletId != request.AcceptedPayerWalletId!.Value.Value ||
            receipt.TargetWalletId != request.RequesterWalletId.Value ||
            receipt.AmountMinor != request.AmountMinor ||
            receipt.CorrelationId != request.Id.Value)
        {
            throw new InvalidOperationException("Reconciliation receipt does not match the payment request.");
        }

        if (receipt.CreatedAtUtc < request.AcceptedAtUtc!.Value)
        {
            throw new InvalidOperationException("Reconciliation receipt predates payment request acceptance.");
        }
    }
}
