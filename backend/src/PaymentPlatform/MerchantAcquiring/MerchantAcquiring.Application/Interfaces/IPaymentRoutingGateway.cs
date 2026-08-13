namespace MerchantAcquiring.Application.Interfaces;

public interface IPaymentRoutingGateway
{
    Task<AcquiringRoutingResult> RouteAsync(
        Guid paymentIntentId,
        string countryCode,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken);
}

public sealed record AcquiringRoutingResult(
    string ProviderId,
    string Rail);
