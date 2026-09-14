using AfriWallet.Transfer.Application;

namespace AfriWallet.PaymentRequests.Application;

public sealed class TransferCorrelationPaymentReceiptReader(ITransferReceiptReader receiptReader)
    : IPaymentRequestPaymentReceiptReader
{
    public async Task<PaymentRequestPaymentReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        var receipt = await receiptReader.FindByCorrelationIdAsync(correlationId, cancellationToken);
        if (receipt is null)
        {
            return null;
        }

        return new PaymentRequestPaymentReceipt(
            receipt.TransferId.Value,
            receipt.SourceWalletId,
            receipt.TargetWalletId,
            receipt.CurrencyCode,
            receipt.AmountMinor,
            receipt.CorrelationId,
            receipt.CreatedAtUtc);
    }
}
