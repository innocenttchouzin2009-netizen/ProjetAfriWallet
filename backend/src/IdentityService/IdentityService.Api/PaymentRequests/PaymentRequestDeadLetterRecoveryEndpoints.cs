using System.Security.Claims;
using AfriWallet.PaymentRequests.Application;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestDeadLetterRecoveryHttpRequest(
    string Reason,
    DateTimeOffset? AvailableAtUtc);

public sealed record PaymentRequestDeadLetterRecoveryHttpResponse(
    Guid EventId,
    string Status,
    int? ReplayOrdinal,
    DateTimeOffset? AvailableAtUtc);

public sealed record PaymentRequestDeadLetterRecoveryErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class PaymentRequestDeadLetterRecoveryErrorCode
{
    public const string Unauthorized = "PAYMENT_REQUEST_DEAD_LETTER_RECOVERY_UNAUTHORIZED";
    public const string Forbidden = "PAYMENT_REQUEST_DEAD_LETTER_RECOVERY_FORBIDDEN";
    public const string ValidationError = "PAYMENT_REQUEST_DEAD_LETTER_RECOVERY_VALIDATION_ERROR";
    public const string NotFound = "PAYMENT_REQUEST_DEAD_LETTER_RECOVERY_NOT_FOUND";
    public const string Conflict = "PAYMENT_REQUEST_DEAD_LETTER_RECOVERY_CONFLICT";
}

public sealed class PaymentRequestDeadLetterRecoveryHttpOptions
{
    private readonly HashSet<Guid> authorizedActorIds;

    public PaymentRequestDeadLetterRecoveryHttpOptions(IEnumerable<Guid> authorizedActorIds)
    {
        ArgumentNullException.ThrowIfNull(authorizedActorIds);
        this.authorizedActorIds = authorizedActorIds
            .Where(x => x != Guid.Empty)
            .ToHashSet();
    }

    public bool IsAuthorized(Guid actorId) =>
        actorId != Guid.Empty && authorizedActorIds.Contains(actorId);

    public static PaymentRequestDeadLetterRecoveryHttpOptions FromConfiguration(IConfiguration? configuration)
    {
        if (configuration is null)
        {
            return new PaymentRequestDeadLetterRecoveryHttpOptions(Array.Empty<Guid>());
        }

        var actorIds = new List<Guid>();
        foreach (var child in configuration.GetSection("PaymentRequests:DeadLetterRecovery:AuthorizedActorIds").GetChildren())
        {
            if (!Guid.TryParse(child.Value, out var actorId) || actorId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "PaymentRequests:DeadLetterRecovery:AuthorizedActorIds contains an invalid actor id.");
            }

            actorIds.Add(actorId);
        }

        return new PaymentRequestDeadLetterRecoveryHttpOptions(actorIds);
    }
}

public static class PaymentRequestDeadLetterRecoveryEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestDeadLetterRecoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/ops/payment-request-event-outbox")
            .RequireAuthorization()
            .MapPost("/dead-letter/{eventId:guid}/recover", RecoverAsync);

        return endpoints;
    }

    private static async Task<IResult> RecoverAsync(
        Guid eventId,
        PaymentRequestDeadLetterRecoveryHttpRequest request,
        ClaimsPrincipal principal,
        PaymentRequestEventDeadLetterRecoveryService recoveryService,
        PaymentRequestDeadLetterRecoveryHttpOptions options,
        TimeProvider timeProvider,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var actorId) || actorId == Guid.Empty)
        {
            return Results.Json(
                new PaymentRequestDeadLetterRecoveryErrorResponse(
                    PaymentRequestDeadLetterRecoveryErrorCode.Unauthorized,
                    "Authenticated actor id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!options.IsAuthorized(actorId))
        {
            return Results.Json(
                new PaymentRequestDeadLetterRecoveryErrorResponse(
                    PaymentRequestDeadLetterRecoveryErrorCode.Forbidden,
                    "The authenticated actor is not authorized for dead-letter recovery.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (eventId == Guid.Empty || request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.BadRequest(new PaymentRequestDeadLetterRecoveryErrorResponse(
                PaymentRequestDeadLetterRecoveryErrorCode.ValidationError,
                "Event id and recovery reason are required.",
                httpContext.TraceIdentifier));
        }

        var requestedAtUtc = timeProvider.GetUtcNow();
        var availableAtUtc = request.AvailableAtUtc ?? requestedAtUtc;

        try
        {
            var result = await recoveryService.RecoverAsync(
                new PaymentRequestEventDeadLetterRecoveryCommand(
                    eventId,
                    actorId.ToString("D"),
                    request.Reason,
                    requestedAtUtc,
                    availableAtUtc),
                cancellationToken);

            var plan = result.Decision?.Plan;
            var response = new PaymentRequestDeadLetterRecoveryHttpResponse(
                eventId,
                result.Code.ToString(),
                plan?.ReplayOrdinal,
                plan?.AvailableAtUtc);

            return result.Code switch
            {
                PaymentRequestEventDeadLetterRecoveryExecutionCode.Requeued =>
                    Results.Ok(response),
                PaymentRequestEventDeadLetterRecoveryExecutionCode.EventNotFound =>
                    Results.NotFound(new PaymentRequestDeadLetterRecoveryErrorResponse(
                        PaymentRequestDeadLetterRecoveryErrorCode.NotFound,
                        "Dead-letter event was not found.",
                        httpContext.TraceIdentifier)),
                PaymentRequestEventDeadLetterRecoveryExecutionCode.NotDeadLetter =>
                    Conflict("Event is not in dead-letter state.", httpContext),
                PaymentRequestEventDeadLetterRecoveryExecutionCode.AttemptHistoryMismatch =>
                    Conflict("Dead-letter attempt history is inconsistent.", httpContext),
                PaymentRequestEventDeadLetterRecoveryExecutionCode.ReplayLimitExceeded =>
                    Conflict("Dead-letter replay limit has been exceeded.", httpContext),
                PaymentRequestEventDeadLetterRecoveryExecutionCode.RequeueConflict =>
                    Conflict("Dead-letter event could not be requeued because its state changed.", httpContext),
                _ => Conflict("Dead-letter recovery could not be completed.", httpContext)
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PaymentRequestDeadLetterRecoveryErrorResponse(
                PaymentRequestDeadLetterRecoveryErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static IResult Conflict(string message, HttpContext httpContext) =>
        Results.Conflict(new PaymentRequestDeadLetterRecoveryErrorResponse(
            PaymentRequestDeadLetterRecoveryErrorCode.Conflict,
            message,
            httpContext.TraceIdentifier));
}
