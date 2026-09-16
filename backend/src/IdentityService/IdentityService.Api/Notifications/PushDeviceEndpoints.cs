using System.Security.Claims;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

namespace IdentityService.Api.Notifications;

public static class PushDeviceEndpoints
{
    public static IEndpointRouteBuilder MapPushDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/push/devices").RequireAuthorization();
        group.MapPost("", RegisterAsync);
        group.MapDelete("/{installationId}", UnregisterAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterPushDeviceHttpRequest request,
        ClaimsPrincipal principal,
        PushDeviceRegistrationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return Results.Unauthorized();

        try
        {
            var platform = ParsePlatform(request.Platform);
            var result = await service.RegisterAsync(
                new RegisterPushDeviceCommand(
                    userId,
                    request.InstallationId,
                    platform,
                    request.PushToken,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            var registration = result.Registration;
            return Results.Ok(new PushDeviceHttpResponse(
                registration.Id.Value,
                registration.InstallationId,
                registration.Platform.ToString().ToLowerInvariant(),
                result.Status.ToString(),
                registration.IsActive,
                registration.UpdatedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PushDeviceErrorResponse(
                "PUSH_DEVICE_VALIDATION_ERROR",
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new PushDeviceErrorResponse(
                "PUSH_DEVICE_CONFLICT",
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static async Task<IResult> UnregisterAsync(
        string installationId,
        ClaimsPrincipal principal,
        PushDeviceRegistrationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
            return Results.Unauthorized();

        try
        {
            var removed = await service.UnregisterAsync(
                userId,
                installationId,
                DateTimeOffset.UtcNow,
                cancellationToken);

            return removed ? Results.NoContent() : Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PushDeviceErrorResponse(
                "PUSH_DEVICE_VALIDATION_ERROR",
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static PushPlatform ParsePlatform(string value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "android" => PushPlatform.Android,
            "ios" => PushPlatform.Ios,
            _ => throw new ArgumentException("Push platform must be 'android' or 'ios'.", nameof(value))
        };

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}

public sealed record RegisterPushDeviceHttpRequest(
    string InstallationId,
    string Platform,
    string PushToken);

public sealed record PushDeviceHttpResponse(
    Guid RegistrationId,
    string InstallationId,
    string Platform,
    string Status,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);

public sealed record PushDeviceErrorResponse(
    string Code,
    string Message,
    string TraceId);
