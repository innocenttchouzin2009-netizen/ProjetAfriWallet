using System.Security.Claims;
using AfriWallet.P2P.Application;
using AfriWallet.P2P.Domain;
using AfriWallet.P2P.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace IdentityService.Api.P2P;

public static class P2PEndpoints
{
    public static IEndpointRouteBuilder MapP2PEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/p2p")
            .RequireAuthorization()
            .MapPost("/transfers", ExecuteAsync);

        return endpoints;
    }

    private static async Task<IResult> ExecuteAsync(
        P2PTransferRequest request,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        IServiceProvider serviceProvider,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new P2PErrorResponse(P2PErrorCode.Unauthorized, "Authenticated user id is missing.", httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (request.SourceWalletId == Guid.Empty ||
            request.CorrelationId == Guid.Empty ||
            request.AmountMinor <= 0 ||
            string.IsNullOrWhiteSpace(request.RecipientKind) ||
            string.IsNullOrWhiteSpace(request.RecipientValue) ||
            string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            return Results.BadRequest(new P2PErrorResponse(
                P2PErrorCode.ValidationError,
                "Source wallet, recipient, currency, positive amount and correlation id are required.",
                httpContext.TraceIdentifier));
        }

        var sourceWallet = await walletRepository.GetAsync(WalletId.From(request.SourceWalletId), cancellationToken);
        if (sourceWallet is null || sourceWallet.OwnerId != userId)
        {
            return Results.NotFound(new P2PErrorResponse(
                P2PErrorCode.NotFound,
                "Source wallet not found.",
                httpContext.TraceIdentifier));
        }

        if (serviceProvider.GetService<IAfWalIdentityDirectory>() is null ||
            serviceProvider.GetService<IQrRecipientDirectory>() is null)
        {
            return Results.Json(
                new P2PErrorResponse(
                    P2PErrorCode.RecipientProvidersUnavailable,
                    "Authoritative AfWal ID and QR recipient providers are not configured.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            var currency = Currency.Create(request.CurrencyCode);
            var recipient = CreateRecipientReference(request.RecipientKind, request.RecipientValue);
            var service = serviceProvider.GetRequiredService<P2PTransferOrchestrationService>();
            var result = await service.ExecuteAsync(
                new ExecuteP2PTransferCommand(
                    request.SourceWalletId,
                    recipient,
                    currency,
                    request.AmountMinor,
                    request.CorrelationId,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            if (result.Status == P2PTransferExecutionStatus.RecipientNotFound || result.Receipt is null)
            {
                return Results.NotFound(new P2PErrorResponse(
                    P2PErrorCode.RecipientNotFound,
                    "Recipient was not found for the requested currency.",
                    httpContext.TraceIdentifier));
            }

            var receipt = result.Receipt;
            return Results.Ok(new P2PTransferResponse(
                receipt.TransferId,
                receipt.SourceWalletId,
                receipt.TargetWalletId,
                receipt.CurrencyCode,
                receipt.AmountMinor,
                receipt.CorrelationId,
                receipt.CreatedAtUtc,
                request.RecipientKind.Trim().ToLowerInvariant()));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new P2PErrorResponse(
                P2PErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound(new P2PErrorResponse(
                P2PErrorCode.NotFound,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new P2PErrorResponse(
                P2PErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static RecipientReference CreateRecipientReference(string kind, string value) =>
        kind.Trim().ToLowerInvariant() switch
        {
            "afwal-id" => RecipientReference.FromAfWalId(value),
            "qr" => RecipientReference.FromQrToken(value),
            _ => throw new ArgumentException("Recipient kind must be 'afwal-id' or 'qr'.", nameof(kind))
        };
}
