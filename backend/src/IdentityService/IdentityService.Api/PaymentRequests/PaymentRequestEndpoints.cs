using System.Security.Claims;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/payment-requests").RequireAuthorization();
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreatePaymentRequestHttpRequest request,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        PaymentRequestApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new PaymentRequestErrorResponse(PaymentRequestErrorCode.Unauthorized, "Authenticated user id is missing.", httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (request.RequesterWalletId == Guid.Empty ||
            request.CorrelationId == Guid.Empty ||
            request.AmountMinor <= 0 ||
            string.IsNullOrWhiteSpace(request.RecipientKind) ||
            string.IsNullOrWhiteSpace(request.RecipientValue) ||
            string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                "Requester wallet, recipient, currency, positive amount and correlation id are required.",
                httpContext.TraceIdentifier));
        }

        var requesterWallet = await walletRepository.GetAsync(WalletId.From(request.RequesterWalletId), cancellationToken);
        if (requesterWallet is null || requesterWallet.OwnerId != userId)
        {
            return Results.NotFound(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.NotFound,
                "Requester wallet not found.",
                httpContext.TraceIdentifier));
        }

        try
        {
            var currency = Currency.Create(request.CurrencyCode);
            if (requesterWallet.Currency != currency)
            {
                throw new ArgumentException("Requester wallet currency must match the payment request currency.", nameof(request.CurrencyCode));
            }

            var recipient = CreateRecipientReference(request.RecipientKind, request.RecipientValue);
            var result = await service.CreateAsync(
                new CreatePaymentRequestCommand(
                    requesterWallet.Id,
                    recipient,
                    currency,
                    request.AmountMinor,
                    request.CorrelationId,
                    DateTimeOffset.UtcNow,
                    request.ExpiresAtUtc),
                cancellationToken);

            return result.Status switch
            {
                CreatePaymentRequestStatus.Created when result.Request is not null =>
                    Results.Created($"/api/v1/payment-requests/{result.Request.Id.Value}", ToResponse(result.Request)),
                CreatePaymentRequestStatus.Existing when result.Request is not null =>
                    Results.Ok(ToResponse(result.Request)),
                CreatePaymentRequestStatus.RecipientNotFound =>
                    Results.NotFound(new PaymentRequestErrorResponse(
                        PaymentRequestErrorCode.RecipientNotFound,
                        "Recipient was not found for the requested currency.",
                        httpContext.TraceIdentifier)),
                CreatePaymentRequestStatus.SelfRequestNotAllowed =>
                    Results.Conflict(new PaymentRequestErrorResponse(
                        PaymentRequestErrorCode.Conflict,
                        "Requester and payer wallets must be different.",
                        httpContext.TraceIdentifier)),
                _ => Results.Conflict(new PaymentRequestErrorResponse(
                    PaymentRequestErrorCode.Conflict,
                    "Payment request could not be created.",
                    httpContext.TraceIdentifier))
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        PaymentRequestApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new PaymentRequestErrorResponse(PaymentRequestErrorCode.Unauthorized, "Authenticated user id is missing.", httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (id == Guid.Empty)
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                "Payment request id is required.",
                httpContext.TraceIdentifier));
        }

        var snapshot = await service.GetAsync(PaymentRequestId.From(id), cancellationToken);
        if (snapshot is null)
        {
            return Results.NotFound(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.NotFound,
                "Payment request not found.",
                httpContext.TraceIdentifier));
        }

        var requesterWallet = await walletRepository.GetAsync(snapshot.RequesterWalletId, cancellationToken);
        if (requesterWallet is null || requesterWallet.OwnerId != userId)
        {
            return Results.NotFound(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.NotFound,
                "Payment request not found.",
                httpContext.TraceIdentifier));
        }

        return Results.Ok(ToResponse(snapshot));
    }

    private static RecipientReference CreateRecipientReference(string kind, string value) =>
        kind.Trim().ToLowerInvariant() switch
        {
            "afwal-id" => RecipientReference.FromAfWalId(value),
            "qr" => RecipientReference.FromQrToken(value),
            _ => throw new ArgumentException("Recipient kind must be 'afwal-id' or 'qr'.", nameof(kind))
        };

    private static PaymentRequestHttpResponse ToResponse(PaymentRequestSnapshot snapshot) => new(
        snapshot.Id.Value,
        snapshot.RequesterWalletId.Value,
        snapshot.PayerReference.Kind == RecipientReferenceKind.AfWalId ? "afwal-id" : "qr",
        snapshot.Currency.Code,
        snapshot.AmountMinor,
        snapshot.CorrelationId,
        snapshot.CreatedAtUtc,
        snapshot.ExpiresAtUtc,
        snapshot.Status.ToString(),
        snapshot.AcceptedPayerWalletId?.Value,
        snapshot.AcceptedAtUtc,
        snapshot.TransferId,
        snapshot.ClosedAtUtc);
}
