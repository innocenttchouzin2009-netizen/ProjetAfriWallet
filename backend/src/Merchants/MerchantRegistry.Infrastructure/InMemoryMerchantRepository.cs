using System.Collections.Concurrent;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Domain.Merchants;

namespace AfriWallet.Merchants.Registry.Infrastructure;

public sealed class InMemoryMerchantRepository : IMerchantRepository
{
    private readonly ConcurrentDictionary<string, Merchant> _items = new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryAdd(merchant.MerchantId.ToString(), merchant))
            throw new InvalidOperationException("Merchant already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[merchant.MerchantId.ToString()] = merchant;
        return Task.CompletedTask;
    }

    public Task<Merchant?> GetAsync(MerchantId merchantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(merchantId.ToString(), out var merchant);
        return Task.FromResult(merchant);
    }

    public Task<Merchant?> GetByOwnerAwidAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var merchant = _items.Values.FirstOrDefault(x => string.Equals(x.OwnerAwid, awid, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(merchant);
    }

    public Task<bool> ExistsByLegalNameAsync(string legalName, string countryCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var exists = _items.Values.Any(x =>
            string.Equals(x.Profile.LegalName, legalName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Profile.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
