using AfriWallet.Merchants.Onboarding.Application.Abstractions;

namespace AfriWallet.Merchants.Onboarding.Infrastructure;

public sealed class SandboxMerchantProfileReader : IMerchantProfileReader
{
    private readonly Dictionary<string, MerchantProfileSnapshot> _items = new(StringComparer.OrdinalIgnoreCase);

    public void Set(MerchantProfileSnapshot snapshot) => _items[snapshot.MerchantId] = snapshot;

    public Task<MerchantProfileSnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(merchantId, out var snapshot);
        return Task.FromResult(snapshot);
    }
}
