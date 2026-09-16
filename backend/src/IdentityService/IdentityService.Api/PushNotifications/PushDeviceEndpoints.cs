using System.Security.Claims;
using AfriWallet.PushNotifications.Application;
using AfriWallet.PushNotifications.Domain;

namespace IdentityService.Api.PushNotifications;

public sealed record RegisterPushDeviceHttpRequest(string DeviceId, string Platform, string PushToken);
public sealed record PushDeviceHttpResponse(Guid Id, string DeviceId, string Platform, string Status, DateTimeOffset RegisteredAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed record PushDeviceErrorResponse(string Code, string Message, string TraceId);

public static class PushDeviceEndpoints
{
    public static IEndpointRouteBuilder MapPushDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/push/devices").RequireAuthorization();
        group.MapPost("/", RegisterAsync);
        group.MapGet("/", ListAsync);
        group.MapDelete("/{deviceId}", RevokeAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterPushDeviceHttpRequest request, ClaimsPrincipal principal, PushDeviceRegistrationService service, HttpContext context, CancellationToken cancellationToken)
    {
        if (!TryUserId(principal, out var userId)) return Unauthorized(context);
        try
        {
            var result = await service.RegisterAsync(new RegisterPushDeviceCommand(userId, request.DeviceId, ParsePlatform(request.Platform), request.PushToken, DateTimeOffset.UtcNow), cancellationToken);
            var response = ToResponse(result.Device);
            return result.Status == RegisterPushDeviceStatus.Created
                ? Results.Created($"/api/v1/push/devices/{Uri.EscapeDataString(response.DeviceId)}", response)
                : Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new PushDeviceErrorResponse("PUSH_VALIDATION_ERROR", ex.Message, context.TraceIdentifier));
        }
    }

    private static async Task<IResult> ListAsync(ClaimsPrincipal principal, PushDeviceRegistrationService service, HttpContext context, CancellationToken cancellationToken)
    {
        if (!TryUserId(principal, out var userId)) return Unauthorized(context);
        var devices = await service.ListAsync(userId, cancellationToken);
        return Results.Ok(devices.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> RevokeAsync(string deviceId, ClaimsPrincipal principal, PushDeviceRegistrationService service, HttpContext context, CancellationToken cancellationToken)
    {
        if (!TryUserId(principal, out var userId)) return Unauthorized(context);
        try
        {
            var revoked = await service.RevokeAsync(userId, deviceId, DateTimeOffset.UtcNow, cancellationToken);
            return revoked is null
                ? Results.NotFound(new PushDeviceErrorResponse("PUSH_DEVICE_NOT_FOUND", "Push device registration not found.", context.TraceIdentifier))
                : Results.Ok(ToResponse(revoked));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new PushDeviceErrorResponse("PUSH_VALIDATION_ERROR", ex.Message, context.TraceIdentifier));
        }
    }

    private static bool TryUserId(ClaimsPrincipal principal, out Guid userId) => Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);
    private static IResult Unauthorized(HttpContext context) => Results.Json(new PushDeviceErrorResponse("PUSH_UNAUTHORIZED", "Authenticated user id is missing.", context.TraceIdentifier), statusCode: StatusCodes.Status401Unauthorized);
    private static PushPlatform ParsePlatform(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "android" => PushPlatform.Android,
        "ios" => PushPlatform.Ios,
        "web" => PushPlatform.Web,
        _ => throw new ArgumentException("Platform must be 'android', 'ios', or 'web'.", nameof(value))
    };
    private static PushDeviceHttpResponse ToResponse(PushDeviceSnapshot value) => new(value.Id, value.DeviceId, value.Platform.ToString().ToLowerInvariant(), value.Status.ToString().ToLowerInvariant(), value.RegisteredAtUtc, value.UpdatedAtUtc, value.RevokedAtUtc);
}
