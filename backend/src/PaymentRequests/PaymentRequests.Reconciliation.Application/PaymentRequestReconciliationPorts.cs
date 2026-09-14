namespace AfriWallet.PaymentRequests.Reconciliation.Application;

public interface ITransferReceiptReader
{
    Task<TransferReceiptSnapshot?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
