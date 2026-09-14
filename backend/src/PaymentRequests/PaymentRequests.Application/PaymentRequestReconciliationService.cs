using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestReconciliationService(
    IPaymentRequestRepository repository,
    IPaymentRequestPaymentReceiptReader paymentReceiptReader)
{
    public async Task<PaymentRequestReconciliationResult> ReconcileAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null)
        {
            return PaymentRequestReconciliationResult.RequestNotFound();
        }

        if (request.Status == PaymentRequestStatus.Paid)
        {
            return PaymentRequestReconciliationResult.AlreadyPaid(request);
        }

        if (request.Status != PaymentRequestStatus.Accepted)
        {
            return PaymentRequestReconciliationResult.NotEligible(request);
        }

        if (request.AcceptedPayerWalletId is null)
        {
            throw new InvalidOperationException("Accepted payment request is missing payer wallet.");
        }

        var receipt = await paymentReceiptReader.FindByCorrelationIdAsync(request.Id.Value, cancellationToken);
        if (receipt is null)
        {
            return PaymentRequestReconciliationResult.TransferNotFound(request);
        }

        ValidateReceipt(request, receipt);
        request.MarkPaid(receipt.TransferId, receipt.CreatedAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return PaymentRequestReconciliationResult.Reconciled(request);
    }

    private static void ValidateReceipt(PaymentRequest request, PaymentRequestPaymentReceipt receipt)
    {
        if (receipt.TransferId == Guid.Empty)
        {
            throw new InvalidOperationException("Transfer receipt id cannot be empty.");
        }

        if (receipt.CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Transfer receipt timestamp must be UTC.");
        }

        if (request.AcceptedAtUtc is null)
        {
            throw new InvalidOperationException("Accepted payment request is missing acceptance timestamp.");
        }

        if (receipt.CreatedAtUtc < request.AcceptedAtUtc.Value)
        {
            throw new InvalidOperationException("Transfer receipt predates payment request acceptance.");
        }

        if (receipt.SourceWalletId != request.AcceptedPayerWalletId!.Value.Value ||
            receipt.TargetWalletId != request.RequesterWalletId.Value ||
            !string.Equals(receipt.CurrencyCode, request.Currency.Code, StringComparison.Ordinal) ||
            receipt.AmountMinor != request.AmountMinor ||
            receipt.CorrelationId != request.Id.Value)
        {
            throw new InvalidOperationException("Transfer receipt does not match the payment request.");
        }
    }
}
