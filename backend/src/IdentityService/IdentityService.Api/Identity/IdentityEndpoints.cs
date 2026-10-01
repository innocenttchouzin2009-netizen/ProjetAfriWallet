using System.Security.Claims;

namespace IdentityService.Api.Identity;

public static class IdentityEndpoints
{
    public const string CurrentProfileRoute = "/api/v1/identity/current-profile";
    public const string CurrentIdentityRoute = "/api/v1/identity/current";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(CurrentProfileRoute, GetCurrentProfileAsync)
            .RequireAuthorization();

        endpoints.MapGet(CurrentIdentityRoute, GetCurrentIdentityAsync)
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

    private static async Task<IResult> GetCurrentIdentityAsync(
        ClaimsPrincipal principal,
        CurrentIdentityReadService identities,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
        {
            return Results.Unauthorized();
        }

        var identity = await identities.ReadAsync(userId, cancellationToken);
        return identity is null
            ? Results.NotFound()
            : Results.Ok(identity);
    }
}
