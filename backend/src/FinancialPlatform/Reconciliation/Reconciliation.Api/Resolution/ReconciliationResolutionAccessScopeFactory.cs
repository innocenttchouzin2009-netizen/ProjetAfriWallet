using System.Security.Claims;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Api.Resolution;

public static class ReconciliationResolutionAccessScopeFactory
{
    public const string AllPartnersClaim = "reconciliation_resolution_all";
    public const string PartnerClaim = "reconciliation_partner";
    public const string AdministratorClaim = "reconciliation_resolution_admin";
    public const string AuditClaim = "reconciliation_resolution_audit";

    public static bool TryCreate(
        ClaimsPrincipal principal,
        out ReconciliationResolutionAccessScope? accessScope)
    {
        var actorId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            accessScope = null;
            return false;
        }

        var administrator = HasTruthyClaim(principal, AdministratorClaim);
        var allPartnersRequested = HasTruthyClaim(principal, AllPartnersClaim);
        var canReadAudit = administrator || HasTruthyClaim(principal, AuditClaim);

        if (administrator && allPartnersRequested)
        {
            accessScope = ReconciliationResolutionAccessScope.ForAllPartners(
                actorId,
                canReadAudit: true);
            return true;
        }

        accessScope = ReconciliationResolutionAccessScope.ForPartners(
            actorId,
            principal.FindAll(PartnerClaim).Select(claim => claim.Value),
            canReadAudit);
        return true;
    }

    private static bool HasTruthyClaim(ClaimsPrincipal principal, string claimType) =>
        principal.FindAll(claimType).Any(claim =>
            string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.Value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claim.Value, "all", StringComparison.OrdinalIgnoreCase));
}
