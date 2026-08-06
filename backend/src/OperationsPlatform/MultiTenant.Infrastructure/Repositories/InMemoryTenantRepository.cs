using System.Collections.Concurrent;
using MultiTenant.Application.Interfaces;
using MultiTenant.Domain.Memberships;
using MultiTenant.Domain.Tenants;

namespace MultiTenant.Infrastructure.Repositories;

public sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly ConcurrentDictionary<Guid, Tenant> _tenants = new();

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_tenants.Values.Any(existing =>
                string.Equals(existing.TenantCode, tenant.TenantCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A tenant with this code already exists.");
        }

        if (!_tenants.TryAdd(tenant.TenantId, tenant))
        {
            throw new InvalidOperationException("The tenant already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Tenant?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenant = _tenants.Values.FirstOrDefault(existing =>
            string.Equals(existing.TenantCode, tenantCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(tenant);
    }

    public Task<IReadOnlyCollection<Tenant>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<Tenant>>(
            _tenants.Values.OrderBy(x => x.TenantCode).ToArray());
    }
}

public sealed class InMemoryTenantMembershipRepository : ITenantMembershipRepository
{
    private readonly ConcurrentDictionary<(Guid TenantId, string SubjectId), TenantMembership> _memberships = new();

    public Task AddAsync(TenantMembership membership, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = (membership.TenantId, membership.SubjectId.ToLowerInvariant());

        if (!_memberships.TryAdd(key, membership))
        {
            throw new InvalidOperationException("The subject is already a member of this tenant.");
        }

        return Task.CompletedTask;
    }

    public Task<TenantMembership?> GetAsync(Guid tenantId, string subjectId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _memberships.TryGetValue((tenantId, subjectId.ToLowerInvariant()), out var membership);
        return Task.FromResult(membership);
    }

    public Task<IReadOnlyCollection<TenantMembership>> ListAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var values = _memberships.Values.Where(x => x.TenantId == tenantId).ToArray();
        return Task.FromResult<IReadOnlyCollection<TenantMembership>>(values);
    }
}