using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using IdentityService.Api.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-push-registration-http-{Guid.NewGuid():N}.db");
try
{
    var keyBase64 = Convert.ToBase64String(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray());
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddDevicePushRegistration($"Data Source={dbPath}", keyBase64);

    Assert(!builder.Services.Any(x => x.ServiceType == typeof(IPushNotificationTransport)),
        "Commit 3 must not wire FCM/APNs or any push notification transport.");

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapDevicePushEndpoints();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<NotificationInboxDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    await app.StartAsync();
    await using var _ = app;

    var anonymous = app.GetTestClient();
    var userA = Guid.NewGuid();
    var userB = Guid.NewGuid();
    var clientA = CreateClient(app, userA);
    var clientB = CreateClient(app, userB);

    await RunAsync("register requires authentication", async () =>
    {
        var response = await anonymous.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterDevicePushHttpRequest("device-a", "android", "token-a"));
        Assert(response.StatusCode == HttpStatusCode.Unauthorized, "Anonymous register must return 401.");
    });

    await RunAsync("register derives UserId exclusively from JWT sub", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterDevicePushHttpRequest("device-a", "android", "token-a"));
        Assert(response.StatusCode == HttpStatusCode.OK, "Register must return 200.");
        var body = await response.Content.ReadFromJsonAsync<DevicePushHttpResponse>();
        Assert(body is not null && body.Status == "Created", "First registration must be Created.");
        Assert(body!.DeviceId == "device-a" && body.Platform == "android", "Registration response mismatch.");

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationInboxDbContext>();
        var stored = await db.DevicePushRegistrations.AsNoTracking().SingleAsync(x => x.DeviceId == "device-a");
        Assert(stored.UserId == userA, "Stored UserId must come from authenticated sub.");
        Assert(stored.ProtectedToken != "token-a", "Raw push token must never be stored.");
        Assert(!stored.ProtectedToken.Contains("token-a", StringComparison.Ordinal), "Protected token must not expose plaintext.");
        Assert(stored.TokenHash.Length == 64, "Token hash must be SHA-256 hex.");
    });

    await RunAsync("idempotent re-register returns Existing", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterDevicePushHttpRequest("device-a", "android", "token-a"));
        var body = await response.Content.ReadFromJsonAsync<DevicePushHttpResponse>();
        Assert(response.StatusCode == HttpStatusCode.OK && body?.Status == "Existing",
            "Same registration must return Existing.");
    });

    await RunAsync("token replacement stays scoped to authenticated user registration", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterDevicePushHttpRequest("device-a", "android", "token-b"));
        var body = await response.Content.ReadFromJsonAsync<DevicePushHttpResponse>();
        Assert(response.StatusCode == HttpStatusCode.OK && body?.Status == "Replaced",
            "Changed token must return Replaced.");

        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<DevicePushRegistrationService>();
        var active = await service.ListActiveAsync(userA);
        Assert(active.Count == 1 && active[0].DeviceId == "device-a", "Exactly one active device must remain.");
    });

    await RunAsync("foreign revoke is hidden with 404", async () =>
    {
        var response = await clientB.DeleteAsync("/api/v1/push/devices/device-a");
        Assert(response.StatusCode == HttpStatusCode.NotFound, "Foreign revoke must return 404.");
    });

    await RunAsync("owner revoke deactivates registration", async () =>
    {
        var response = await clientA.DeleteAsync("/api/v1/push/devices/device-a");
        Assert(response.StatusCode == HttpStatusCode.NoContent, "Owner revoke must return 204.");

        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<DevicePushRegistrationService>();
        var active = await service.ListActiveAsync(userA);
        Assert(active.Count == 0, "Revoked device must no longer be active.");
    });

    await RunAsync("unsupported platform returns 400", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterDevicePushHttpRequest("device-b", "windows", "token-x"));
        Assert(response.StatusCode == HttpStatusCode.BadRequest, "Unsupported platform must return 400.");
    });

    Console.WriteLine("AFW-BE-PUSH-NOTIFICATION-1 protected device push registration HTTP scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

static HttpClient CreateClient(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
}

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class HeaderTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) ||
            !Guid.TryParse(raw.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
