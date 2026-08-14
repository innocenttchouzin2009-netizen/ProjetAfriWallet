using System.Collections.Concurrent;
using AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;
using ComplianceProfileDomain = AfriWallet.CompliancePlatform.ComplianceProfile.Domain.ComplianceProfile;

namespace AfriWallet.CompliancePlatform.ComplianceProfile.Infrastructure.Repositories;

public sealed class InMemoryComplianceProfileRepository : IComplianceProfileRepository
{
    private readonly ConcurrentDictionary<Guid, ComplianceProfileDomain> _items = new();

    public Task AddAsync(ComplianceProfileDomain profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_items.TryAdd(profile.ProfileId, profile))
        {
            throw new InvalidOperationException("Compliance profile already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<ComplianceProfileDomain?> GetAsync(Guid profileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(profileId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<IReadOnlyCollection<ComplianceProfileDomain>> ListByCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<ComplianceProfileDomain>>(
            _items.Values
                .Where(x => string.Equals(x.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
                .ToArray());
    }

    public Task<IReadOnlyCollection<ComplianceProfileDomain>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<ComplianceProfileDomain>>(_items.Values.ToArray());
    }
}
