using System.Security.Claims;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Api.Resolution;

public static class ReconciliationResolutionAccessScopeFactory
{
    public const string AllPartnersClaim = "reconciliation_resolution_all";
    public const string PartnerClaim = "reconciliation_partner";

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

        var allPartners = principal.FindAll(AllPartnersClaim)
            .Any(claim =>
                string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Value, "all", StringComparison.OrdinalIgnoreCase));

        if (allPartners)
        {
            accessScope = ReconciliationResolutionAccessScope.ForAllPartners(actorId);
            return true;
        }

        accessScope = ReconciliationResolutionAccessScope.ForPartners(
            actorId,
            principal.FindAll(PartnerClaim).Select(claim => claim.Value));
        return true;
    }
}
