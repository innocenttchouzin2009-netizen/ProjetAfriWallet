using AfriWallet.Merchants.Capture.Application;
using AfriWallet.Merchants.Capture.Domain;
using AfriWallet.Merchants.Receivables.Application;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;

namespace AfriWallet.Merchants.Receivables.Infrastructure;

public sealed class MerchantCaptureReceivableReader(IMerchantCaptureRepository captures)
    : IMerchantCaptureReceivableReader
{
    public async Task<CapturedMerchantPaymentSnapshot?> GetAsync(
        Guid captureExecutionId,
        CancellationToken cancellationToken = default)
    {
        var value = await captures.GetAsync(captureExecutionId, cancellationToken);
        return value is null ? null : new CapturedMerchantPaymentSnapshot(
            value.ExecutionId,
            value.DecisionId,
            value.PaymentIntentId,
            value.MerchantId,
            value.AmountMinor,
            value.Currency,
            value.Status.ToString(),
            value.ProviderReference);
    }
}

public sealed class MerchantRegistryReceivableReader(IMerchantRepository merchants)
    : IMerchantRegistryReceivableReader
{
    public async Task<MerchantReceivableRegistrySnapshot?> GetAsync(
        string merchantId,
        CancellationToken cancellationToken = default)
    {
        MerchantId id;
        try
        {
            id = new MerchantId(merchantId);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var value = await merchants.GetAsync(id, cancellationToken);
        return value is null ? null : new MerchantReceivableRegistrySnapshot(
            value.MerchantId.Value,
            value.Status.ToString(),
            value.Profile.SettlementCurrency);
    }
}
