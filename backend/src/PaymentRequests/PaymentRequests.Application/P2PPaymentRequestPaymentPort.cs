using AfriWallet.P2P.Application;

namespace AfriWallet.PaymentRequests.Application;

public sealed class P2PPaymentRequestPaymentPort(IP2PTransferPort transferPort) : IPaymentRequestPaymentPort
{
    public async Task<PaymentRequestPaymentReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var receipt = await transferPort.ExecuteAsync(
            sourceWalletId,
            targetWalletId,
            amountMinor,
            correlationId,
            requestedAtUtc,
            cancellationToken);

        return new PaymentRequestPaymentReceipt(
            receipt.TransferId,
            receipt.SourceWalletId,
            receipt.TargetWalletId,
            receipt.AmountMinor,
            receipt.CorrelationId,
            receipt.CreatedAtUtc);
    }
}
