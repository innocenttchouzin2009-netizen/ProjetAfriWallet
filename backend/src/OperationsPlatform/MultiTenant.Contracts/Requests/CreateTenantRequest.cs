namespace MultiTenant.Contracts.Requests;

public sealed record CreateTenantRequest(
    string TenantCode,
    string LegalName,
    string DisplayName,
    string CountryCode,
    string BaseCurrency,
    string AdministratorSubjectId);

public sealed record AddTenantMemberRequest(
    string SubjectId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateTenantBrandingRequest(
    string? LogoUrl,
    string? PrimaryColor);

public sealed record UpdateTenantQuotasRequest(
    int ApiRequestsPerMinute,
    int MaximumUsers);