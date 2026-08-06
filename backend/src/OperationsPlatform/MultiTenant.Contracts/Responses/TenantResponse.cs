using MultiTenant.Domain.Tenants;

namespace MultiTenant.Contracts.Responses;

public sealed record TenantResponse(
    Guid TenantId,
    string TenantCode,
    string LegalName,
    string DisplayName,
    string CountryCode,
    string BaseCurrency,
    string Status,
    string? LogoUrl,
    string? PrimaryColor,
    int ApiRequestsPerMinute,
    int MaximumUsers,
    IReadOnlyCollection<string> AllowedCountries,
    IReadOnlyCollection<string> AllowedCurrencies,
    IReadOnlyCollection<string> AllowedLanguages,
    IReadOnlyCollection<string> EnabledFeatures)
{
    public static TenantResponse From(Tenant tenant)
    {
        return new TenantResponse(
            tenant.TenantId,
            tenant.TenantCode,
            tenant.LegalName,
            tenant.DisplayName,
            tenant.CountryCode,
            tenant.BaseCurrency,
            tenant.Status.ToString().ToUpperInvariant(),
            tenant.LogoUrl,
            tenant.PrimaryColor,
            tenant.ApiRequestsPerMinute,
            tenant.MaximumUsers,
            tenant.AllowedCountries,
            tenant.AllowedCurrencies,
            tenant.AllowedLanguages,
            tenant.EnabledFeatures);
    }
}