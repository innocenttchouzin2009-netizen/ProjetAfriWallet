using System.Security.Claims;
using AfriWallet.Notifications.Application;

namespace IdentityService.Api.Notifications;

public static class DevicePushEndpoints
{
    public static IEndpointRouteBuilder MapDevicePushEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/push/devices").RequireAuthorization();
        group.MapPost("", RegisterAsync);
        group.MapDelete("/{deviceId}", RevokeAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDevicePushHttpRequest request,
        ClaimsPrincipal principal,
        DevicePushRegistrationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return Results.Unauthorized();

        try
        {
            var result = await service.RegisterAsync(
                new RegisterDevicePushTokenCommand(
                    userId,
                    request.DeviceId,
                    ParsePlatform(request.Platform),
                    request.Token,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            var registration = result.Registration;
            return Results.Ok(new DevicePushHttpResponse(
                registration.Id,
                registration.DeviceId,
                registration.Platform.ToString().ToLowerInvariant(),
                result.Status.ToString(),
                registration.RevokedAtUtc is null,
                registration.LastSeenAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new DevicePushErrorResponse(
                "PUSH_DEVICE_VALIDATION_ERROR",
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new DevicePushErrorResponse(
                "PUSH_DEVICE_CONFLICT",
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static async Task<IResult> RevokeAsync(
        string deviceId,
        ClaimsPrincipal principal,
        DevicePushRegistrationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return Results.Unauthorized();

        try
        {
            var revoked = await service.RevokeAsync(
                userId,
                deviceId,
                DateTimeOffset.UtcNow,
                cancellationToken);
            return revoked ? Results.NoContent() : Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new DevicePushErrorResponse(
                "PUSH_DEVICE_VALIDATION_ERROR",
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static PushDevicePlatform ParsePlatform(string value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "android" => PushDevicePlatform.Android,
            "ios" => PushDevicePlatform.Ios,
            "web" => PushDevicePlatform.Web,
            _ => throw new ArgumentException("Push platform must be 'android', 'ios' or 'web'.", nameof(value))
        };

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}

public sealed record RegisterDevicePushHttpRequest(string DeviceId, string Platform, string Token);

public sealed record DevicePushHttpResponse(
    Guid RegistrationId,
    string DeviceId,
    string Platform,
    string Status,
    bool IsActive,
    DateTimeOffset LastSeenAtUtc);

public sealed record DevicePushErrorResponse(string Code, string Message, string TraceId);
