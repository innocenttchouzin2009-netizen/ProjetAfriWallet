namespace MultiTenant.Application.Security;

public sealed record TenantContext(
    Guid TenantId,
    string SubjectId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public bool HasPermission(string permission)
    {
        return Roles.Contains("TENANT_SUPER_ADMIN", StringComparer.OrdinalIgnoreCase) ||
               Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}

public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> Storage = new();

    public TenantContext? Current
    {
        get => Storage.Value;
        set => Storage.Value = value;
    }
}