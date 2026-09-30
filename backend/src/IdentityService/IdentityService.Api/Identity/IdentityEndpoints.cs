using System.Security.Claims;
using AfriWallet.P2P.Directory.Persistence;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Identity;

public sealed record CurrentIdentityResponse(Guid UserId, string AfWalId);

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity")
            .RequireAuthorization();

        group.MapGet("/current", GetCurrentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCurrentAsync(
        ClaimsPrincipal principal,
        ICurrentAfWalIdentityReader reader,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new AuthErrorResponse(
                    AuthErrorCode.TokenInvalid,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var current = await reader.FindByOwnerIdAsync(userId, cancellationToken);
        return current is null
            ? Results.NotFound()
            : Results.Ok(new CurrentIdentityResponse(current.UserId, current.AfWalId));
    }
}
