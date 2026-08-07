namespace MultiTenant.Domain.Memberships;

public sealed class TenantMembership
{
    private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public TenantMembership(Guid membershipId, Guid tenantId, string subjectId)
    {
        if (membershipId == Guid.Empty)
        {
            throw new ArgumentException("Membership ID is required.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID is required.");
        }

        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new ArgumentException("Subject ID is required.");
        }

        MembershipId = membershipId;
        TenantId = tenantId;
        SubjectId = subjectId.Trim();
    }

    public Guid MembershipId { get; }

    public Guid TenantId { get; }

    public string SubjectId { get; }

    public MembershipStatus Status { get; private set; } = MembershipStatus.Active;

    public IReadOnlyCollection<string> Roles => _roles.ToArray();

    public IReadOnlyCollection<string> Permissions => _permissions.ToArray();

    public void AddRole(string role)
    {
        EnsureActive();
        _roles.Add(RequireValue(role));
    }

    public void RemoveRole(string role)
    {
        EnsureActive();
        _roles.Remove(role);
    }

    public void GrantPermission(string permission)
    {
        EnsureActive();
        _permissions.Add(RequireValue(permission));
    }

    public void RevokePermission(string permission)
    {
        EnsureActive();
        _permissions.Remove(permission);
    }

    public bool HasPermission(string permission)
    {
        return Status is MembershipStatus.Active &&
               (_permissions.Contains(permission) ||
                _roles.Contains(TenantRoles.SuperAdmin));
    }

    public void Suspend()
    {
        Status = MembershipStatus.Suspended;
    }

    public void Activate()
    {
        Status = MembershipStatus.Active;
    }

    private void EnsureActive()
    {
        if (Status is not MembershipStatus.Active)
        {
            throw new InvalidOperationException("The tenant membership is not active.");
        }
    }

    private static string RequireValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A role or permission value is required.");
        }

        return value.Trim();
    }
}

public enum MembershipStatus
{
    Active,
    Suspended,
    Revoked
}

public static class TenantRoles
{
    public const string SuperAdmin = "TENANT_SUPER_ADMIN";
    public const string Administrator = "TENANT_ADMIN";
    public const string Operations = "TENANT_OPERATIONS";
    public const string Support = "TENANT_SUPPORT";
    public const string Auditor = "TENANT_AUDITOR";
    public const string ReadOnly = "TENANT_READ_ONLY";
}

public static class TenantPermissions
{
    public const string TenantRead = "tenant.read";
    public const string TenantWrite = "tenant.write";
    public const string MemberRead = "tenant.members.read";
    public const string MemberWrite = "tenant.members.write";
    public const string FeatureWrite = "tenant.features.write";
    public const string QuotaWrite = "tenant.quotas.write";
    public const string BrandingWrite = "tenant.branding.write";
    public const string AuditRead = "tenant.audit.read";
}