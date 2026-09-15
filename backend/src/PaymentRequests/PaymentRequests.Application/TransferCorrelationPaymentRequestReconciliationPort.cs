using AfriWallet.Transfer.Application;

namespace AfriWallet.PaymentRequests.Application;

public sealed class TransferCorrelationPaymentRequestReconciliationPort(
    TransferCorrelationLookupService lookupService)
    : IPaymentRequestReconciliationPort
{
    public async Task<PaymentRequestPaymentReceipt?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await lookupService.FindAsync(correlationId, cancellationToken);
        if (result.Status == TransferCorrelationLookupStatus.NotFound)
            return null;

        var receipt = result.Receipt
            ?? throw new InvalidOperationException("Transfer correlation lookup returned Found without a receipt.");

        return new PaymentRequestPaymentReceipt(
            receipt.TransferId.Value,
            receipt.SourceWalletId,
            receipt.TargetWalletId,
            receipt.AmountMinor,
            receipt.CorrelationId,
            receipt.CreatedAtUtc);
    }
}
