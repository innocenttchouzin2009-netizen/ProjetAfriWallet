using System.Security.Claims;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace IdentityService.Api.Transfer;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/transfers")
            .RequireAuthorization();

        group.MapPost("/internal", ExecuteInternalAsync);
        group.MapGet("/by-correlation/{correlationId:guid}/receipt", GetReceiptByCorrelationAsync);

        return endpoints;
    }

    private static async Task<IResult> ExecuteInternalAsync(
        InternalTransferRequest request,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        InternalTransferOrchestrationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new TransferErrorResponse(TransferErrorCode.Unauthorized, "Authenticated user id is missing.", httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (request.SourceWalletId == Guid.Empty || request.TargetWalletId == Guid.Empty || request.CorrelationId == Guid.Empty || request.AmountMinor <= 0)
        {
            return Results.BadRequest(new TransferErrorResponse(
                TransferErrorCode.ValidationError,
                "Source wallet, target wallet, positive amount and correlation id are required.",
                httpContext.TraceIdentifier));
        }

        var sourceWallet = await walletRepository.GetAsync(WalletId.From(request.SourceWalletId), cancellationToken);
        if (sourceWallet is null || sourceWallet.OwnerId != userId)
        {
            return Results.NotFound(new TransferErrorResponse(
                TransferErrorCode.NotFound,
                "Source wallet not found.",
                httpContext.TraceIdentifier));
        }

        try
        {
            var result = await service.ExecuteAsync(
                new ExecuteInternalTransferCommand(
                    request.SourceWalletId,
                    request.TargetWalletId,
                    request.AmountMinor,
                    request.CorrelationId,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Ok(new InternalTransferResponse(
                result.Intent.Id.Value,
                result.Intent.SourceWalletId,
                result.Intent.TargetWalletId,
                result.Intent.CurrencyCode,
                result.Intent.AmountMinor,
                result.Intent.CorrelationId,
                result.JournalEntry.Id.Value,
                result.JournalEntry.PostedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new TransferErrorResponse(
                TransferErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound(new TransferErrorResponse(
                TransferErrorCode.NotFound,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new TransferErrorResponse(
                TransferErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static async Task<IResult> GetReceiptByCorrelationAsync(
        Guid correlationId,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        TransferCorrelationLookupService lookupService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new TransferErrorResponse(TransferErrorCode.Unauthorized, "Authenticated user id is missing.", httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (correlationId == Guid.Empty)
        {
            return Results.BadRequest(new TransferErrorResponse(
                TransferErrorCode.ValidationError,
                "Correlation id is required.",
                httpContext.TraceIdentifier));
        }

        try
        {
            var result = await lookupService.FindAsync(correlationId, cancellationToken);
            if (result.Status == TransferCorrelationLookupStatus.NotFound || result.Receipt is null)
            {
                return Results.NotFound(new TransferErrorResponse(
                    TransferErrorCode.NotFound,
                    "Transfer receipt not found.",
                    httpContext.TraceIdentifier));
            }

            var receipt = result.Receipt;
            var sourceWallet = await walletRepository.GetAsync(WalletId.From(receipt.SourceWalletId), cancellationToken);
            var targetWallet = await walletRepository.GetAsync(WalletId.From(receipt.TargetWalletId), cancellationToken);
            var canRead = sourceWallet?.OwnerId == userId || targetWallet?.OwnerId == userId;
            if (!canRead)
            {
                return Results.NotFound(new TransferErrorResponse(
                    TransferErrorCode.NotFound,
                    "Transfer receipt not found.",
                    httpContext.TraceIdentifier));
            }

            return Results.Ok(new TransferReceiptResponse(
                receipt.TransferId.Value,
                receipt.SourceWalletId,
                receipt.TargetWalletId,
                receipt.CurrencyCode,
                receipt.AmountMinor,
                receipt.CorrelationId,
                receipt.JournalEntryId.Value,
                receipt.CreatedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new TransferErrorResponse(
                TransferErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new TransferErrorResponse(
                TransferErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }
}
