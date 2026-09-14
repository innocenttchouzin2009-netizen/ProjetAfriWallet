namespace AfriWallet.Transfer.Application;

public sealed class TransferCorrelationLookupService(ITransferReceiptReader receiptReader)
{
    public async Task<TransferCorrelationLookupResult> FindAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));

        cancellationToken.ThrowIfCancellationRequested();

        var receipt = await receiptReader.FindByCorrelationIdAsync(correlationId, cancellationToken);
        if (receipt is null)
            return TransferCorrelationLookupResult.NotFound();

        if (receipt.CorrelationId != correlationId)
            throw new InvalidOperationException("Transfer receipt correlation id does not match the requested correlation id.");

        return TransferCorrelationLookupResult.Found(receipt);
    }
}
