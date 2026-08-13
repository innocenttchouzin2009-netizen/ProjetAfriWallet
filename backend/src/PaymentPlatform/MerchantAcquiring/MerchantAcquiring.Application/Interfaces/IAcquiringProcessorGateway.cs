namespace MerchantAcquiring.Application.Interfaces;

public interface IAcquiringProcessorGateway
{
    Task<string> AuthorizeAsync(
        Guid paymentId,
        string providerId,
        long amountMinor,
        string currencyCode,
        CancellationToken cancellationToken);

    Task CaptureAsync(
        string providerReference,
        CancellationToken cancellationToken);

    Task RefundAsync(
        string providerReference,
        long amountMinor,
        CancellationToken cancellationToken);
}
