using AfriWallet.Merchants.Registry.Domain.Merchants;

namespace AfriWallet.Merchants.Registry.Application.Abstractions;

public interface IMerchantRepository
{
    Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default);
    Task SaveAsync(Merchant merchant, CancellationToken cancellationToken = default);
    Task<Merchant?> GetAsync(MerchantId merchantId, CancellationToken cancellationToken = default);
    Task<Merchant?> GetByOwnerAwidAsync(string awid, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLegalNameAsync(string legalName, string countryCode, CancellationToken cancellationToken = default);
}
