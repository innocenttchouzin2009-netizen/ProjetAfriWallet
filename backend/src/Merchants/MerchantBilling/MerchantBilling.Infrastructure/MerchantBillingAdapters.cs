using AfriWallet.Merchants.Billing.Application;
using AfriWallet.Merchants.Capture.Application;
using AfriWallet.Merchants.Receivables.Application;

namespace AfriWallet.Merchants.Billing.Infrastructure;

public sealed class MerchantCaptureBillingReader(IMerchantCaptureRepository captures)
    : IMerchantBillingCaptureReader
{
    public async Task<MerchantBillingCaptureSnapshot?> GetAsync(
        Guid captureExecutionId,
        CancellationToken cancellationToken = default)
    {
        var capture = await captures.GetAsync(captureExecutionId, cancellationToken);
        return capture is null
            ? null
            : new MerchantBillingCaptureSnapshot(
                capture.ExecutionId,
                capture.MerchantId,
                capture.Status.ToString(),
                capture.SettlementReady);
    }
}

public sealed class MerchantReceivableBillingPort(MerchantReceivableService receivables)
    : IMerchantBillingReceivablePort
{
    public async Task<MerchantBillingReceivableSnapshot> CreateFromCaptureAsync(
        Guid captureExecutionId,
        string idempotencyKey,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var value = await receivables.CreateFromCaptureAsync(
            new CreateMerchantReceivableCommand(captureExecutionId, idempotencyKey, actor),
            cancellationToken);

        return new MerchantBillingReceivableSnapshot(
            value.ReceivableId,
            value.CaptureExecutionId,
            value.MerchantId,
            value.GrossAmountMinor,
            value.FeeAmountMinor,
            value.NetAmountMinor,
            value.Currency,
            value.Status.ToString());
    }
}
