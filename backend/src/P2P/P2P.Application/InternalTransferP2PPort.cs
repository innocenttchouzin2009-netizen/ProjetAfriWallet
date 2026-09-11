using AfriWallet.Transfer.Application;

namespace AfriWallet.P2P.Application;

public sealed class InternalTransferP2PPort(InternalTransferOrchestrationService transferService) : IP2PTransferPort
{
    public async Task<P2PTransferReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var result = await transferService.ExecuteAsync(
            new ExecuteInternalTransferCommand(
                sourceWalletId,
                targetWalletId,
                amountMinor,
                correlationId,
                requestedAtUtc),
            cancellationToken);

        var intent = result.Intent;
        return new P2PTransferReceipt(
            intent.Id.Value,
            intent.SourceWalletId,
            intent.TargetWalletId,
            intent.CurrencyCode,
            intent.AmountMinor,
            intent.CorrelationId,
            intent.CreatedAtUtc);
    }
}
