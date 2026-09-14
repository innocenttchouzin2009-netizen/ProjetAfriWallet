using AfriWallet.PaymentRequests.Reconciliation.Application;
using CertifiedTransferReceiptReader = AfriWallet.Transfer.Application.ITransferReceiptReader;

namespace AfriWallet.PaymentRequests.Reconciliation.Infrastructure;

public sealed class TransferReceiptReaderAdapter(CertifiedTransferReceiptReader inner)
    : AfriWallet.PaymentRequests.Reconciliation.Application.ITransferReceiptReader
{
    public async Task<TransferReceiptSnapshot?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));

        cancellationToken.ThrowIfCancellationRequested();
        var receipt = await inner.FindByCorrelationIdAsync(correlationId, cancellationToken);
        if (receipt is null)
            return null;

        return new TransferReceiptSnapshot(
            receipt.TransferId.Value,
            receipt.SourceWalletId,
            receipt.TargetWalletId,
            receipt.AmountMinor,
            receipt.CorrelationId,
            receipt.CreatedAtUtc);
    }
}
