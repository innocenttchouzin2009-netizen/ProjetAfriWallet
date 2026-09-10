using System.Security.Claims;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace IdentityService.Api.Transfer;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/transfers")
            .RequireAuthorization()
            .MapPost("/internal", ExecuteInternalAsync);

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
}
