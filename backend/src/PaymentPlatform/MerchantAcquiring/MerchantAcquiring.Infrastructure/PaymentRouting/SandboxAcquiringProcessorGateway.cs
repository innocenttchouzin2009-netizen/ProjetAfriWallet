using MerchantAcquiring.Application.Interfaces;

namespace MerchantAcquiring.Infrastructure.PaymentRouting;

public sealed class SandboxAcquiringProcessorGateway :
    IAcquiringProcessorGateway
{
    public Task<string> AuthorizeAsync(
        Guid paymentId,
        string providerId,
        long amountMinor,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            $"auth-{providerId}-{paymentId:N}");
    }

    public Task CaptureAsync(
        string providerReference,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RefundAsync(
        string providerReference,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
