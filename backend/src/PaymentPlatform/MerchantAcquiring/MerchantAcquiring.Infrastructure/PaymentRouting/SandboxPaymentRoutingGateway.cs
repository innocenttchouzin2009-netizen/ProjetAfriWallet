using MerchantAcquiring.Application.Interfaces;

namespace MerchantAcquiring.Infrastructure.PaymentRouting;

public sealed class SandboxPaymentRoutingGateway :
    IPaymentRoutingGateway
{
    public Task<AcquiringRoutingResult> RouteAsync(
        Guid paymentIntentId,
        string countryCode,
        string currencyCode,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        var provider =
            countryCode.Equals(
                "CM",
                StringComparison.OrdinalIgnoreCase)
            ? "AFW-WALLET"
            : "CARD-SANDBOX";

        return Task.FromResult(
            new AcquiringRoutingResult(
                provider,
                "SANDBOX"));
    }
}
