namespace MerchantAcquiring.Application.Interfaces;

public interface IMerchantRegistryGateway
{
    Task<bool> IsMerchantActiveAsync(
        string merchantId,
        CancellationToken cancellationToken);
}
