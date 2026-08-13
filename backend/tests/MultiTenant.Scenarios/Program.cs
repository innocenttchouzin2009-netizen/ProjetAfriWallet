using MultiTenant.Application.Services;
using MultiTenant.Domain.Memberships;
using MultiTenant.Infrastructure.Repositories;

var tenantRepository = new InMemoryTenantRepository();
var membershipRepository = new InMemoryTenantMembershipRepository();

var service = new TenantAdministrationService(tenantRepository, membershipRepository);

var tenant = await service.CreateTenantAsync(
    "afriwallet-cameroon",
    "AfriWallet Cameroon SA",
    "AfriWallet Cameroon",
    "CM",
    "XAF",
    "awid-admin-001",
    CancellationToken.None);

Assert(tenant.TenantCode == "afriwallet-cameroon", "tenant creation");

tenant.Activate();

Assert(tenant.Status.ToString() == "Active", "tenant activation");

tenant.AllowCurrency("EUR");
tenant.AllowLanguage("fr");
tenant.AllowLanguage("sw");
tenant.EnableFeature("MobileMoney");
tenant.EnableFeature("MerchantQr");

Assert(tenant.AllowedCurrencies.Contains("EUR"), "currency configuration");
Assert(tenant.AllowedLanguages.Contains("sw"), "language configuration");
Assert(tenant.EnabledFeatures.Contains("MerchantQr"), "feature configuration");

tenant.UpdateQuotas(apiRequestsPerMinute: 500, maximumUsers: 250);

Assert(tenant.MaximumUsers == 250, "quota configuration");

tenant.UpdateBranding(
    "https://sandbox.afriwallet.example/logo.png",
    "#064E3B");

Assert(tenant.PrimaryColor == "#064E3B", "tenant branding");

var member = await service.AddMemberAsync(
    tenant.TenantId,
    "awid-agent-001",
    [TenantRoles.Support],
    [TenantPermissions.TenantRead, TenantPermissions.MemberRead],
    CancellationToken.None);

Assert(member.HasPermission(TenantPermissions.TenantRead), "tenant membership");

var crossTenantBlocked = false;

try
{
    await service.EnsureTenantAccessAsync(
        tenant.TenantId,
        Guid.NewGuid(),
        CancellationToken.None);
}
catch (TenantBoundaryViolationException)
{
    crossTenantBlocked = true;
}

Assert(crossTenantBlocked, "cross-tenant access blocked");

tenant.Suspend();

Assert(tenant.Status.ToString() == "Suspended", "tenant suspension");

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0012.5 multi-tenant administration scenarios passed.");

static void Assert(bool condition, string scenario)
{
    if (!condition)
    {
        Console.WriteLine($"{scenario} ........ FAIL");
        Environment.ExitCode = 1;
        throw new InvalidOperationException($"Scenario failed: {scenario}");
    }

    Console.WriteLine($"{scenario} ........ PASS");
}
