using System.Security.Claims;
using AfriWallet.Wallet.Application;

namespace IdentityService.Api.Wallet;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/wallets")
            .RequireAuthorization();

        group.MapPost("/", CreateWalletAsync);
        group.MapGet("/", ListWalletsAsync);
        group.MapGet("/{walletId:guid}", GetWalletAsync);
        group.MapPost("/{walletId:guid}/suspend", SuspendWalletAsync);
        group.MapPost("/{walletId:guid}/activate", ActivateWalletAsync);
        group.MapPost("/{walletId:guid}/close", CloseWalletAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateWalletAsync(
        CreateWalletRequest request,
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        var result = await service.CreateAsync(
            new CreateWalletCommand(userId, request.CurrencyCode, request.CountryCode),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return ToHttpResult(result, httpContext, success => Results.Created($"/api/v1/wallets/{success.WalletId}", success));
    }

    private static async Task<IResult> ListWalletsAsync(
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        var result = await service.ListByOwnerAsync(userId, cancellationToken);
        return ToHttpResult(result, httpContext, Results.Ok);
    }

    private static async Task<IResult> GetWalletAsync(
        Guid walletId,
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        var result = await service.GetAsync(walletId, cancellationToken);
        if (result.Succeeded && result.Value is not null && result.Value.OwnerId != userId)
        {
            return Results.NotFound(new WalletErrorResponse(WalletErrorCode.NotFound, "Wallet not found.", httpContext.TraceIdentifier));
        }

        return ToHttpResult(result, httpContext, Results.Ok);
    }

    private static Task<IResult> SuspendWalletAsync(
        Guid walletId,
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        TransitionOwnedWalletAsync(walletId, principal, service, httpContext, cancellationToken, service.SuspendAsync);

    private static Task<IResult> ActivateWalletAsync(
        Guid walletId,
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        TransitionOwnedWalletAsync(walletId, principal, service, httpContext, cancellationToken, service.ActivateAsync);

    private static Task<IResult> CloseWalletAsync(
        Guid walletId,
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        TransitionOwnedWalletAsync(walletId, principal, service, httpContext, cancellationToken, service.CloseAsync);

    private static async Task<IResult> TransitionOwnedWalletAsync(
        Guid walletId,
        ClaimsPrincipal principal,
        WalletRegistryApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        Func<Guid, DateTimeOffset, CancellationToken, Task<WalletOperationResult<WalletView>>> transition)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        var existing = await service.GetAsync(walletId, cancellationToken);
        if (!existing.Succeeded || existing.Value is null)
        {
            return ToHttpResult(existing, httpContext, Results.Ok);
        }

        if (existing.Value.OwnerId != userId)
        {
            return Results.NotFound(new WalletErrorResponse(WalletErrorCode.NotFound, "Wallet not found.", httpContext.TraceIdentifier));
        }

        var result = await transition(walletId, DateTimeOffset.UtcNow, cancellationToken);
        return ToHttpResult(result, httpContext, Results.Ok);
    }

    private static IResult ToHttpResult<T>(
        WalletOperationResult<T> result,
        HttpContext httpContext,
        Func<T, IResult> onSuccess)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return onSuccess(result.Value);
        }

        var error = new WalletErrorResponse(
            result.ErrorCode ?? WalletErrorCode.ValidationError,
            result.ErrorMessage ?? "Wallet operation failed.",
            httpContext.TraceIdentifier);

        return result.ErrorCode switch
        {
            WalletErrorCode.NotFound => Results.NotFound(error),
            WalletErrorCode.DuplicateWallet => Results.Conflict(error),
            WalletErrorCode.UnsupportedCurrency => Results.BadRequest(error),
            WalletErrorCode.InvalidTransition => Results.Conflict(error),
            _ => Results.BadRequest(error)
        };
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);

    private static IResult Unauthorized(HttpContext httpContext) =>
        Results.Json(
            new WalletErrorResponse("WALLET_UNAUTHORIZED", "Authenticated user id is missing.", httpContext.TraceIdentifier),
            statusCode: StatusCodes.Status401Unauthorized);
}
