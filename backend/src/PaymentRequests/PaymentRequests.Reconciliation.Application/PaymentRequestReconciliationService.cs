using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Reconciliation.Application;

public sealed class PaymentRequestReconciliationService(
    IPaymentRequestRepository repository,
    ITransferReceiptReader transferReceiptReader,
    IPaymentRequestReconciliationRecordRepository reconciliationRecords)
{
    public async Task<PaymentRequestReconciliationResult> ReconcileAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = await repository.GetAsync(id, cancellationToken);
        if (request is null)
            return await PersistAsync(id, PaymentRequestReconciliationResult.NotFound(), cancellationToken);

        if (request.Status == PaymentRequestStatus.Paid)
            return await PersistAsync(id, PaymentRequestReconciliationResult.AlreadyPaid(request), cancellationToken);

        if (request.Status != PaymentRequestStatus.Accepted || request.AcceptedPayerWalletId is null)
            return await PersistAsync(id, PaymentRequestReconciliationResult.NotEligible(request), cancellationToken);

        var receipt = await transferReceiptReader.FindByCorrelationIdAsync(request.Id.Value, cancellationToken);
        if (receipt is null)
            return await PersistAsync(id, PaymentRequestReconciliationResult.TransferReceiptNotFound(request), cancellationToken);

        if (receipt.CorrelationId != request.Id.Value ||
            receipt.SourceWalletId != request.AcceptedPayerWalletId.Value.Value ||
            receipt.TargetWalletId != request.RequesterWalletId.Value ||
            receipt.AmountMinor != request.AmountMinor)
            throw new InvalidOperationException("Transfer receipt does not match the accepted payment request.");

        request.MarkPaid(receipt.TransferId, receipt.CreatedAtUtc);
        await repository.UpdateAsync(request, cancellationToken);
        return await PersistAsync(id, PaymentRequestReconciliationResult.Reconciled(request), cancellationToken);
    }

    private async Task<PaymentRequestReconciliationResult> PersistAsync(
        PaymentRequestId id,
        PaymentRequestReconciliationResult result,
        CancellationToken cancellationToken)
    {
        await reconciliationRecords.UpsertAsync(
            new PaymentRequestReconciliationRecord(id, result.Status, result.Request?.TransferId),
            cancellationToken);
        return result;
    }
}
