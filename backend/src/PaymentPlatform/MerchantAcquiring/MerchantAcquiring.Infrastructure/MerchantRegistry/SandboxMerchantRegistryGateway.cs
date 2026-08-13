using MerchantAcquiring.Application.Interfaces;

namespace MerchantAcquiring.Infrastructure.MerchantRegistry;

public sealed class SandboxMerchantRegistryGateway :
    IMerchantRegistryGateway
{
    public Task<bool> IsMerchantActiveAsync(
        string merchantId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(merchantId) &&
            !merchantId.StartsWith(
                "DISABLED",
                StringComparison.OrdinalIgnoreCase));
    }
}
