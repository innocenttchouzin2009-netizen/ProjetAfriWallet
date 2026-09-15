using System.Security.Claims;
using AfriWallet.Notifications.Application;

namespace IdentityService.Api.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications").RequireAuthorization();
        group.MapGet("", ListAsync);
        group.MapGet("/unread-count", CountUnreadAsync);
        group.MapGet("/{notificationId:guid}", GetAsync);
        group.MapPost("/{notificationId:guid}/read", MarkReadAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        bool? unreadOnly,
        int? limit,
        string? cursor,
        ClaimsPrincipal principal,
        InAppNotificationInboxService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        try
        {
            var page = await service.ListAsync(userId, unreadOnly ?? false, limit ?? 50, cursor, cancellationToken);
            return Results.Ok(page);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new NotificationErrorResponse("NOTIFICATION_VALIDATION_ERROR", exception.Message));
        }
    }

    private static async Task<IResult> CountUnreadAsync(
        ClaimsPrincipal principal,
        InAppNotificationInboxService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var count = await service.CountUnreadAsync(userId, cancellationToken);
        return Results.Ok(new NotificationUnreadCountResponse(count));
    }

    private static async Task<IResult> GetAsync(
        Guid notificationId,
        ClaimsPrincipal principal,
        InAppNotificationInboxService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var item = await service.GetAsync(userId, notificationId, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> MarkReadAsync(
        Guid notificationId,
        ClaimsPrincipal principal,
        InAppNotificationInboxService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var item = await service.MarkReadAsync(userId, notificationId, DateTimeOffset.UtcNow, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}

public sealed record NotificationUnreadCountResponse(int Count);
public sealed record NotificationErrorResponse(string Code, string Message);
