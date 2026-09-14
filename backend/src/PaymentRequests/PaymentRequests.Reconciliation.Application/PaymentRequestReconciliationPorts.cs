namespace AfriWallet.PaymentRequests.Reconciliation.Application;

public interface ITransferReceiptReader
{
    Task<TransferReceiptSnapshot?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestReconciliationRecordRepository
{
    Task<PaymentRequestReconciliationRecord?> GetAsync(
        AfriWallet.PaymentRequests.Domain.PaymentRequestId requestId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        PaymentRequestReconciliationRecord record,
        CancellationToken cancellationToken = default);
}
