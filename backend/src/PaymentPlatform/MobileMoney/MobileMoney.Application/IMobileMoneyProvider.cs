using AfriWallet.PaymentPlatform.MobileMoney.Domain;

namespace AfriWallet.PaymentPlatform.MobileMoney.Application;

public interface IMobileMoneyProvider
{
    MobileMoneyProvider Definition { get; }

    Task<ProviderPaymentResult> InitiateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderStatusResult> GetStatusAsync(
        string providerReference,
        CancellationToken cancellationToken = default);

    Task<MobileMoneyPaymentStatus> ProcessCallbackAsync(
        MobileMoneyCallback callback,
        CancellationToken cancellationToken = default);
}