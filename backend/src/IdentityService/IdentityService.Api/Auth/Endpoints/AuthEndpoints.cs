using System.Security.Claims;
using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;

namespace IdentityService.Api.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(AuthRoutes.Register, RegisterAsync).AllowAnonymous();
        endpoints.MapPost(AuthRoutes.Login, LoginAsync).AllowAnonymous();
        endpoints.MapPost(AuthRoutes.Refresh, RefreshAsync).AllowAnonymous();
        endpoints.MapPost(AuthRoutes.Logout, LogoutAsync).RequireAuthorization();
        endpoints.MapPost(AuthRoutes.LogoutAll, LogoutAllAsync).RequireAuthorization();
        endpoints.MapGet(AuthRoutes.Session, GetSessionAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AuthApplicationService auth,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToHttpResult(await auth.RegisterAsync(request, cancellationToken), context, StatusCodes.Status201Created);

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AuthApplicationService auth,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToHttpResult(await auth.LoginAsync(request, cancellationToken), context);

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        AuthApplicationService auth,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ToHttpResult(await auth.RefreshAsync(request, cancellationToken), context);

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal principal,
        AuthApplicationService auth,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetPrincipalIds(principal, out _, out var sessionId))
        {
            return Unauthorized(context, AuthErrorCode.TokenInvalid, "Invalid access token principal.");
        }

        return ToHttpResult(
            await auth.LogoutAsync(sessionId, cancellationToken),
            context,
            StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> LogoutAllAsync(
        ClaimsPrincipal principal,
        AuthApplicationService auth,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetPrincipalIds(principal, out var userId, out _))
        {
            return Unauthorized(context, AuthErrorCode.TokenInvalid, "Invalid access token principal.");
        }

        return ToHttpResult(
            await auth.LogoutAllAsync(userId, cancellationToken),
            context,
            StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> GetSessionAsync(
        ClaimsPrincipal principal,
        AuthApplicationService auth,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetPrincipalIds(principal, out var userId, out var sessionId))
        {
            return Unauthorized(context, AuthErrorCode.TokenInvalid, "Invalid access token principal.");
        }

        return ToHttpResult(
            await auth.GetSessionAsync(userId, sessionId, cancellationToken),
            context);
    }

    private static bool TryGetPrincipalIds(
        ClaimsPrincipal principal,
        out Guid userId,
        out Guid sessionId)
    {
        userId = Guid.Empty;
        sessionId = Guid.Empty;

        return Guid.TryParse(principal.FindFirstValue("sub"), out userId) &&
               Guid.TryParse(principal.FindFirstValue("sid"), out sessionId);
    }

    private static IResult ToHttpResult<T>(
        AuthOperationResult<T> result,
        HttpContext context,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Succeeded)
        {
            return successStatusCode == StatusCodes.Status204NoContent
                ? Results.NoContent()
                : Results.Json(result.Value, statusCode: successStatusCode);
        }

        var statusCode = result.ErrorCode switch
        {
            AuthErrorCode.ValidationError => StatusCodes.Status400BadRequest,
            AuthErrorCode.IdentifierAlreadyExists => StatusCodes.Status409Conflict,
            AuthErrorCode.UserDisabled => StatusCodes.Status403Forbidden,
            AuthErrorCode.InvalidCredentials or
            AuthErrorCode.TokenInvalid or
            AuthErrorCode.TokenExpired or
            AuthErrorCode.SessionExpired or
            AuthErrorCode.SessionRevoked or
            AuthErrorCode.RefreshInvalid or
            AuthErrorCode.RefreshExpired or
            AuthErrorCode.RefreshReused => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(
            new AuthErrorResponse(
                result.ErrorCode ?? AuthErrorCode.ValidationError,
                result.ErrorMessage ?? "Authentication request failed.",
                context.TraceIdentifier),
            statusCode: statusCode);
    }

    private static IResult Unauthorized(HttpContext context, string code, string message) =>
        Results.Json(
            new AuthErrorResponse(code, message, context.TraceIdentifier),
            statusCode: StatusCodes.Status401Unauthorized);
}
