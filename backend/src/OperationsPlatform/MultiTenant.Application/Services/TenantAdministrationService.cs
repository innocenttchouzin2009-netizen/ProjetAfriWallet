using MultiTenant.Application.Interfaces;
using MultiTenant.Domain.Memberships;
using MultiTenant.Domain.Tenants;

namespace MultiTenant.Application.Services;

public sealed class TenantAdministrationService
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantMembershipRepository _memberships;

    public TenantAdministrationService(
        ITenantRepository tenants,
        ITenantMembershipRepository memberships)
    {
        _tenants = tenants;
        _memberships = memberships;
    }

    public async Task<Tenant> CreateTenantAsync(
        string tenantCode,
        string legalName,
        string displayName,
        string countryCode,
        string baseCurrency,
        string administratorSubjectId,
        CancellationToken cancellationToken)
    {
        var tenant = new Tenant(
            Guid.NewGuid(),
            tenantCode,
            legalName,
            displayName,
            countryCode,
            baseCurrency);

        await _tenants.AddAsync(tenant, cancellationToken);

        var membership = new TenantMembership(
            Guid.NewGuid(),
            tenant.TenantId,
            administratorSubjectId);

        membership.AddRole(TenantRoles.SuperAdmin);

        foreach (var permission in AllPermissions())
        {
            membership.GrantPermission(permission);
        }

        await _memberships.AddAsync(membership, cancellationToken);

        return tenant;
    }

    public async Task<TenantMembership> AddMemberAsync(
        Guid tenantId,
        string subjectId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken)
    {
        var tenant = await RequireTenantAsync(tenantId, cancellationToken);

        if (tenant.Status is not TenantStatus.Active)
        {
            throw new InvalidOperationException("Members can only be added to an active tenant.");
        }

        var currentCount = (await _memberships.ListAsync(tenantId, cancellationToken)).Count;

        if (currentCount >= tenant.MaximumUsers)
        {
            throw new InvalidOperationException("The tenant user quota has been reached.");
        }

        var membership = new TenantMembership(
            Guid.NewGuid(),
            tenantId,
            subjectId);

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            membership.AddRole(role);
        }

        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            membership.GrantPermission(permission);
        }

        await _memberships.AddAsync(membership, cancellationToken);

        return membership;
    }

    public async Task EnsureTenantAccessAsync(
        Guid requestedTenantId,
        Guid resourceTenantId,
        CancellationToken cancellationToken)
    {
        await RequireTenantAsync(requestedTenantId, cancellationToken);

        if (requestedTenantId != resourceTenantId)
        {
            throw new TenantBoundaryViolationException(requestedTenantId, resourceTenantId);
        }
    }

    public Task<Tenant?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _tenants.GetAsync(tenantId, cancellationToken);
    }

    private async Task<Tenant> RequireTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _tenants.GetAsync(tenantId, cancellationToken)
               ?? throw new KeyNotFoundException("Tenant not found.");
    }

    private static IEnumerable<string> AllPermissions()
    {
        yield return TenantPermissions.TenantRead;
        yield return TenantPermissions.TenantWrite;
        yield return TenantPermissions.MemberRead;
        yield return TenantPermissions.MemberWrite;
        yield return TenantPermissions.FeatureWrite;
        yield return TenantPermissions.QuotaWrite;
        yield return TenantPermissions.BrandingWrite;
        yield return TenantPermissions.AuditRead;
    }
}

public sealed class TenantBoundaryViolationException : Exception
{
    public TenantBoundaryViolationException(Guid requestedTenantId, Guid resourceTenantId)
        : base($"Cross-tenant access denied. Requested tenant '{requestedTenantId}' cannot access resource tenant '{resourceTenantId}'.")
    {
        RequestedTenantId = requestedTenantId;
        ResourceTenantId = resourceTenantId;
    }

    public Guid RequestedTenantId { get; }

    public Guid ResourceTenantId { get; }
}