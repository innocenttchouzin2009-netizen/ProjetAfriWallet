using MultiTenant.Domain.Memberships;
using MultiTenant.Domain.Tenants;

namespace MultiTenant.Application.Interfaces;

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);

    Task<Tenant?> GetAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<Tenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Tenant>> ListAsync(CancellationToken cancellationToken);
}

public interface ITenantMembershipRepository
{
    Task AddAsync(TenantMembership membership, CancellationToken cancellationToken);

    Task<TenantMembership?> GetAsync(Guid tenantId, string subjectId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TenantMembership>> ListAsync(Guid tenantId, CancellationToken cancellationToken);
}