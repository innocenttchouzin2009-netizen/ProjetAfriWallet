namespace AfriWallet.Transfer.Application;

public interface ITransferReceiptReader
{
    Task<TransferReceiptReadModel?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
