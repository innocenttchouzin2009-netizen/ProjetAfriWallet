using System.Security.Claims;

namespace MobileMoney.Production.Authentication;

public static class MobileMoneyAuthenticatedUser
{
    public static bool TryGetAfrikaWalletUserId(
        this ClaimsPrincipal principal,
        out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirst("sub")?.Value;
        return Guid.TryParse(subject, out userId);
    }
}
