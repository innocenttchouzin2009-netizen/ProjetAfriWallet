using System.Security.Claims;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

namespace IdentityService.Api.Notifications;

public static class NotificationPreferenceEndpoints
{
    public static IEndpointRouteBuilder MapNotificationPreferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notification-preferences")
            .RequireAuthorization();

        group.MapGet("", GetAsync);
        group.MapPut("/{channel}", UpdateAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal principal,
        NotificationPreferenceApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(principal, out var userId))
            return Unauthorized(httpContext);

        var preferences = await service.GetAsync(userId, cancellationToken);
        return Results.Ok(preferences.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> UpdateAsync(
        string channel,
        UpdateNotificationPreferenceRequest request,
        ClaimsPrincipal principal,
        NotificationPreferenceApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(principal, out var userId))
            return Unauthorized(httpContext);

        if (!TryParseChannel(channel, out var parsedChannel))
        {
            return Results.BadRequest(new NotificationPreferenceErrorResponse(
                NotificationPreferenceErrorCode.ValidationError,
                "Notification channel must be 'in-app' or 'push'.",
                httpContext.TraceIdentifier));
        }

        try
        {
            var result = await service.UpdateAsync(
                new UpdateNotificationPreferenceCommand(
                    userId,
                    parsedChannel,
                    request.IsEnabled,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Ok(new NotificationPreferenceUpdateResponse(
                result.Status.ToString().ToLowerInvariant(),
                ToResponse(result.Preference)));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new NotificationPreferenceErrorResponse(
                NotificationPreferenceErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new NotificationPreferenceErrorResponse(
                NotificationPreferenceErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static bool TryGetAuthenticatedUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;

    private static bool TryParseChannel(string value, out NotificationChannel channel)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "in-app":
                channel = NotificationChannel.InApp;
                return true;
            case "push":
                channel = NotificationChannel.Push;
                return true;
            default:
                channel = default;
                return false;
        }
    }

    private static NotificationPreferenceResponse ToResponse(NotificationPreferenceSnapshot preference) => new(
        preference.Channel switch
        {
            NotificationChannel.InApp => "in-app",
            NotificationChannel.Push => "push",
            _ => throw new ArgumentOutOfRangeException(nameof(preference.Channel))
        },
        preference.IsEnabled,
        preference.CreatedAtUtc,
        preference.UpdatedAtUtc);

    private static IResult Unauthorized(HttpContext httpContext) => Results.Json(
        new NotificationPreferenceErrorResponse(
            NotificationPreferenceErrorCode.Unauthorized,
            "Authenticated user id is missing.",
            httpContext.TraceIdentifier),
        statusCode: StatusCodes.Status401Unauthorized);
}
