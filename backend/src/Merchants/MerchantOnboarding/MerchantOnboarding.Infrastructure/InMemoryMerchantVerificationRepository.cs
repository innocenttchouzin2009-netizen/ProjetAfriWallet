using System.Collections.Concurrent;
using AfriWallet.Merchants.Onboarding.Application.Abstractions;
using AfriWallet.Merchants.Onboarding.Domain.Cases;

namespace AfriWallet.Merchants.Onboarding.Infrastructure;

public sealed class InMemoryMerchantVerificationRepository : IMerchantVerificationRepository
{
    private readonly ConcurrentDictionary<Guid, MerchantVerificationCase> _items = new();

    public Task AddAsync(MerchantVerificationCase verification, CancellationToken cancellationToken = default)
    {
        if (!_items.TryAdd(verification.VerificationId, verification))
            throw new InvalidOperationException("Merchant verification already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(MerchantVerificationCase verification, CancellationToken cancellationToken = default)
    {
        _items[verification.VerificationId] = verification;
        return Task.CompletedTask;
    }

    public Task<MerchantVerificationCase?> GetAsync(Guid verificationId, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(verificationId, out var result);
        return Task.FromResult(result);
    }

    public Task<MerchantVerificationCase?> GetByMerchantAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        var result = _items.Values.FirstOrDefault(x => string.Equals(x.MerchantId, merchantId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result);
    }
}
