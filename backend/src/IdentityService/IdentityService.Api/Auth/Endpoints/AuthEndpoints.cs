using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Contracts;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Security;

namespace IdentityService.Api.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(AuthRoutes.Register, RegisterAsync);
        endpoints.MapPost(AuthRoutes.Login, LoginAsync);
        endpoints.MapPost(AuthRoutes.Refresh, RefreshAsync);
        endpoints.MapPost(AuthRoutes.Logout, LogoutAsync);
        endpoints.MapPost(AuthRoutes.LogoutAll, LogoutAllAsync);
        endpoints.MapGet(AuthRoutes.Session, GetSessionAsync);
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
        AuthApplicationService auth,
        JwtAccessTokenValidator validator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var principalResult = await ResolveActivePrincipalAsync(auth, validator, context, cancellationToken);
        if (!principalResult.Succeeded)
        {
            return principalResult.Error!;
        }

        var result = await auth.LogoutAsync(principalResult.Principal!.SessionId, cancellationToken);
        return ToHttpResult(result, context, StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> LogoutAllAsync(
        AuthApplicationService auth,
        JwtAccessTokenValidator validator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var principalResult = await ResolveActivePrincipalAsync(auth, validator, context, cancellationToken);
        if (!principalResult.Succeeded)
        {
            return principalResult.Error!;
        }

        var result = await auth.LogoutAllAsync(principalResult.Principal!.UserId, cancellationToken);
        return ToHttpResult(result, context, StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> GetSessionAsync(
        AuthApplicationService auth,
        JwtAccessTokenValidator validator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var principalResult = await ResolveActivePrincipalAsync(auth, validator, context, cancellationToken);
        if (!principalResult.Succeeded)
        {
            return principalResult.Error!;
        }

        var principal = principalResult.Principal!;
        return ToHttpResult(
            await auth.GetSessionAsync(principal.UserId, principal.SessionId, cancellationToken),
            context);
    }

    private static async Task<PrincipalResolution> ResolveActivePrincipalAsync(
        AuthApplicationService auth,
        JwtAccessTokenValidator validator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return PrincipalResolution.Failure(Unauthorized(context, AuthErrorCode.TokenInvalid, "Bearer access token required."));
        }

        var token = authorization["Bearer ".Length..].Trim();
        var principal = validator.Validate(token);
        if (principal is null)
        {
            return PrincipalResolution.Failure(Unauthorized(context, AuthErrorCode.TokenInvalid, "Invalid access token."));
        }

        var session = await auth.GetSessionAsync(principal.UserId, principal.SessionId, cancellationToken);
        if (!session.Succeeded || session.Value is null)
        {
            return PrincipalResolution.Failure(ToHttpResult(session, context));
        }

        if (session.Value.TokenVersion != principal.TokenVersion)
        {
            return PrincipalResolution.Failure(Unauthorized(context, AuthErrorCode.TokenInvalid, "Stale access token."));
        }

        return PrincipalResolution.Success(principal);
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

    private sealed record PrincipalResolution(
        bool Succeeded,
        AuthAccessTokenPrincipal? Principal,
        IResult? Error)
    {
        public static PrincipalResolution Success(AuthAccessTokenPrincipal principal) => new(true, principal, null);
        public static PrincipalResolution Failure(IResult error) => new(false, null, error);
    }
}
