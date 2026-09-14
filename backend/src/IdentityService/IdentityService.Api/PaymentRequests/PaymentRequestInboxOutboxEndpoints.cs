using System.Security.Claims;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestInboxOutboxEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestInboxOutboxEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/payment-requests").RequireAuthorization();
        group.MapGet("/inbox", ListInboxAsync);
        group.MapGet("/outbox", ListOutboxAsync);
        return endpoints;
    }

    private static Task<IResult> ListInboxAsync(
        ClaimsPrincipal principal,
        AuthorizedPaymentRequestQueryService service,
        HttpRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            principal,
            request,
            httpContext,
            (userId, statuses, page, order, token) => service.ListInboxAsync(
                new AuthorizedInboxQuery(userId, statuses, page, order), token),
            cancellationToken);

    private static Task<IResult> ListOutboxAsync(
        ClaimsPrincipal principal,
        AuthorizedPaymentRequestQueryService service,
        HttpRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            principal,
            request,
            httpContext,
            (userId, statuses, page, order, token) => service.ListOutboxAsync(
                new AuthorizedOutboxQuery(userId, statuses, page, order), token),
            cancellationToken);

    private static async Task<IResult> ExecuteAsync(
        ClaimsPrincipal principal,
        HttpRequest request,
        HttpContext httpContext,
        Func<Guid, IReadOnlyCollection<PaymentRequestStatus>?, PaymentRequestPageRequest, PaymentRequestTemporalOrder, CancellationToken, Task<PaymentRequestQueryPage>> query,
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
            var page = PaymentRequestPageRequest.Create(
                ParseInt(request, "page", 0),
                ParseInt(request, "pageSize", 20));
            var statuses = ParseStatuses(request);
            var order = ParseOrder(request);

            var result = await query(userId, statuses, page, order, cancellationToken);
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

    private static PaymentRequestInboxOutboxPageResponse ToResponse(PaymentRequestQueryPage page) => new(
        page.Items.Select(item => new PaymentRequestInboxOutboxItemResponse(
            item.Id.Value,
            item.RequesterWalletId.Value,
            item.PayerReferenceKind == RecipientReferenceKind.AfWalId ? "afwal-id" : "qr",
            item.Currency.Code,
            item.AmountMinor,
            item.CreatedAtUtc,
            item.ExpiresAtUtc,
            item.UpdatedAtUtc,
            item.Status.ToString(),
            item.ClosedAtUtc)).ToArray(),
        page.PageNumber,
        page.PageSize,
        page.TotalCount,
        page.HasMore);
}
