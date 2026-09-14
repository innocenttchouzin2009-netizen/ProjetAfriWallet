using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Reconciliation.Application;

public sealed class PaymentRequestReconciliationService(
    IPaymentRequestRepository repository,
    ITransferReceiptReader transferReceiptReader)
{
    public async Task<PaymentRequestReconciliationResult> ReconcileAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null) return PaymentRequestReconciliationResult.NotFound();
        if (request.Status == PaymentRequestStatus.Paid) return PaymentRequestReconciliationResult.AlreadyPaid(request);
        if (request.Status != PaymentRequestStatus.Accepted || request.AcceptedPayerWalletId is null)
            return PaymentRequestReconciliationResult.NotEligible(request);

        var receipt = await transferReceiptReader.FindByCorrelationIdAsync(request.Id.Value, cancellationToken);
        if (receipt is null) return PaymentRequestReconciliationResult.TransferReceiptNotFound(request);

        if (receipt.CorrelationId != request.Id.Value ||
            receipt.SourceWalletId != request.AcceptedPayerWalletId.Value.Value ||
            receipt.TargetWalletId != request.RequesterWalletId.Value ||
            receipt.AmountMinor != request.AmountMinor)
            throw new InvalidOperationException("Transfer receipt does not match the accepted payment request.");

        request.MarkPaid(receipt.TransferId, receipt.CreatedAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return PaymentRequestReconciliationResult.Reconciled(request);
    }
}
