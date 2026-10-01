using System.Security.Claims;

namespace IdentityService.Api.Identity;

public static class IdentityEndpoints
{
    public const string CurrentProfileRoute = "/api/v1/identity/current-profile";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(CurrentProfileRoute, GetCurrentProfileAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> GetCurrentProfileAsync(
        ClaimsPrincipal principal,
        CurrentProfileReadService profiles,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
        {
            return Results.Unauthorized();
        }

        var profile = await profiles.ReadAsync(userId, cancellationToken);
        return profile is null
            ? Results.NotFound()
            : Results.Ok(profile);
    }
}
