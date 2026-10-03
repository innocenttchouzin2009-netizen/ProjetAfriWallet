using System.Security.Claims;
using AfriWallet.Merchant.Application.Contracts.QrPayments;
using AfriWallet.Merchant.Application.QrPayments;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Merchant.Api.QrPayments;

public sealed record InitiateQrPaymentHttpRequest(
    string QrId,
    string PayerWalletId,
    long AmountMinor,
    string Currency,
    string IdempotencyKey);

public sealed record QrPaymentErrorResponse(
    string Code,
    string Message,
    string? TraceId);

public static class QrPaymentContractEndpoints
{
    public static IEndpointRouteBuilder MapQrPaymentContractEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/qr-payments");

        group.MapPost("/decode", DecodeAsync);
        group.MapPost("/initiate", InitiateAsync)
            .RequireAuthorization();
        group.MapGet("/transfers/{transferIntentId}/status", GetStatusAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> DecodeAsync(
        DecodeQrRequest request,
        QrPaymentContractService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.DecodeAsync(request, cancellationToken));
        }
        catch (QrPaymentContractException exception)
        {
            return Error(exception, httpContext);
        }
    }

    private static async Task<IResult> InitiateAsync(
        InitiateQrPaymentHttpRequest request,
        ClaimsPrincipal principal,
        QrPaymentContractService service,
        IWalletRepository wallets,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Json(
                new QrPaymentErrorResponse(
                    QrPaymentContractValues.ErrorCodes.Unauthorized,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!Guid.TryParse(request.PayerWalletId, out var walletGuid) ||
            walletGuid == Guid.Empty)
        {
            return Results.BadRequest(
                new QrPaymentErrorResponse(
                    QrPaymentContractValues.ErrorCodes.Validation,
                    "Payer wallet identifier is invalid.",
                    httpContext.TraceIdentifier));
        }

        var wallet = await wallets.GetAsync(
            WalletId.From(walletGuid),
            cancellationToken);

        if (wallet is null || wallet.OwnerId != userId)
        {
            return Results.NotFound(
                new QrPaymentErrorResponse(
                    QrPaymentContractValues.ErrorCodes.PayerWalletNotFound,
                    "Payer wallet was not found.",
                    httpContext.TraceIdentifier));
        }

        if (wallet.Status != WalletStatus.Active)
        {
            return Results.Conflict(
                new QrPaymentErrorResponse(
                    QrPaymentContractValues.ErrorCodes.PayerWalletUnavailable,
                    "Payer wallet is not active.",
                    httpContext.TraceIdentifier));
        }

        if (!string.Equals(
                wallet.Currency.Code,
                request.Currency?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new QrPaymentErrorResponse(
                    QrPaymentContractValues.ErrorCodes.Validation,
                    "Payer wallet currency does not match the requested currency.",
                    httpContext.TraceIdentifier));
        }

        try
        {
            var result = await service.InitiateAsync(
                new InitiateQrPaymentRequest(
                    request.QrId,
                    request.PayerWalletId,
                    request.AmountMinor,
                    request.Currency,
                    request.IdempotencyKey,
                    userId),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (QrPaymentContractException exception)
        {
            return Error(exception, httpContext);
        }
    }

    private static async Task<IResult> GetStatusAsync(
        string transferIntentId,
        ClaimsPrincipal principal,
        QrPaymentContractService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Json(
                new QrPaymentErrorResponse(
                    QrPaymentContractValues.ErrorCodes.Unauthorized,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var result = await service.GetStatusAsync(
                userId,
                transferIntentId,
                cancellationToken);
            return Results.Ok(result);
        }
        catch (QrPaymentContractException exception)
        {
            return Error(exception, httpContext);
        }
    }

    private static IResult Error(
        QrPaymentContractException exception,
        HttpContext httpContext)
    {
        var response = new QrPaymentErrorResponse(
            exception.Code,
            exception.Message,
            httpContext.TraceIdentifier);

        return exception.Code switch
        {
            QrPaymentContractValues.ErrorCodes.NotFound =>
                Results.NotFound(response),
            QrPaymentContractValues.ErrorCodes.IdempotencyConflict =>
                Results.Conflict(response),
            QrPaymentContractValues.ErrorCodes.NotActive =>
                Results.Conflict(response),
            QrPaymentContractValues.ErrorCodes.Expired =>
                Results.Conflict(response),
            _ => Results.BadRequest(response)
        };
    }

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);
}
