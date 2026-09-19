using System.Security.Claims;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestHistoryEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/payment-requests")
            .RequireAuthorization()
            .MapGet("/history", ListAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        PaymentRequestHistoryService service,
        HttpRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId) || userId == Guid.Empty)
        {
            return Results.Json(
                new PaymentRequestErrorResponse(
                    PaymentRequestErrorCode.Unauthorized,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var direction = ParseDirection(request);
            var statuses = ParseStatuses(request);
            var page = PaymentRequestPageRequest.Create(
                ParseInt(request, "page", 0),
                ParseInt(request, "pageSize", 20));
            var order = ParseOrder(request);

            var result = await service.ListAsync(
                new PaymentRequestHistoryQuery(userId, direction, statuses, page, order),
                cancellationToken);

            return Results.Ok(ToResponse(result));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static PaymentRequestHistoryDirection ParseDirection(HttpRequest request)
    {
        if (!request.Query.TryGetValue("direction", out var raw) || string.IsNullOrWhiteSpace(raw.ToString()))
        {
            return PaymentRequestHistoryDirection.All;
        }

        return raw.ToString().Trim().ToLowerInvariant() switch
        {
            "all" => PaymentRequestHistoryDirection.All,
            "sent" => PaymentRequestHistoryDirection.Sent,
            "received" => PaymentRequestHistoryDirection.Received,
            _ => throw new ArgumentException("Query parameter 'direction' must be 'all', 'sent' or 'received'.", "direction")
        };
    }

    private static int ParseInt(HttpRequest request, string key, int defaultValue)
    {
        if (!request.Query.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw.ToString()))
        {
            return defaultValue;
        }

        if (!int.TryParse(raw.ToString(), out var value))
        {
            throw new ArgumentException($"Query parameter '{key}' must be an integer.", key);
        }

        return value;
    }

    private static IReadOnlyCollection<PaymentRequestStatus>? ParseStatuses(HttpRequest request)
    {
        if (!request.Query.TryGetValue("status", out var rawValues) || rawValues.Count == 0)
        {
            return null;
        }

        var statuses = new HashSet<PaymentRequestStatus>();
        foreach (var rawValue in rawValues)
        {
            foreach (var token in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<PaymentRequestStatus>(token, true, out var status) || !Enum.IsDefined(status))
                {
                    throw new ArgumentException($"Unsupported payment request status '{token}'.", "status");
                }

                statuses.Add(status);
            }
        }

        return statuses.Count == 0 ? null : statuses.ToArray();
    }

    private static PaymentRequestTemporalOrder ParseOrder(HttpRequest request)
    {
        if (!request.Query.TryGetValue("order", out var raw) || string.IsNullOrWhiteSpace(raw.ToString()))
        {
            return PaymentRequestTemporalOrder.NewestFirst;
        }

        return raw.ToString().Trim().ToLowerInvariant() switch
        {
            "newest" or "newest-first" => PaymentRequestTemporalOrder.NewestFirst,
            "oldest" or "oldest-first" => PaymentRequestTemporalOrder.OldestFirst,
            _ => throw new ArgumentException("Query parameter 'order' must be 'newest' or 'oldest'.", "order")
        };
    }

    private static PaymentRequestHistoryPageResponse ToResponse(PaymentRequestHistoryPage page) => new(
        page.Items.Select(item => new PaymentRequestHistoryItemResponse(
            item.Request.Id.Value,
            item.Direction switch
            {
                PaymentRequestHistoryDirection.Sent => "sent",
                PaymentRequestHistoryDirection.Received => "received",
                _ => "all"
            },
            item.Request.PayerReferenceKind == RecipientReferenceKind.AfWalId ? "afwal-id" : "qr",
            item.Request.Currency.Code,
            item.Request.AmountMinor,
            item.Request.CreatedAtUtc,
            item.Request.ExpiresAtUtc,
            item.Request.UpdatedAtUtc,
            item.Request.Status.ToString(),
            item.Request.ClosedAtUtc)).ToArray(),
        page.PageNumber,
        page.PageSize,
        page.TotalCount,
        page.HasMore);
}
