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

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-push-http-{Guid.NewGuid():N}.db");
try
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Services
        .AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddPushDeviceRegistration($"Data Source={dbPath}");

    Assert(!builder.Services.Any(x => x.ServiceType == typeof(IPushDeliveryPort)),
        "Commit 3 must not wire a real push delivery provider.");

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapPushDeviceEndpoints();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PushDeviceRegistrationDbContext>();
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
            new RegisterPushDeviceHttpRequest("device-a", "android", "token-a"));
        Assert(response.StatusCode == HttpStatusCode.Unauthorized, "Anonymous register must return 401.");
    });

    await RunAsync("register derives user exclusively from authenticated sub", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterPushDeviceHttpRequest("device-a", "android", "token-a"));
        Assert(response.StatusCode == HttpStatusCode.OK, "Register must return 200.");
        var body = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse>();
        Assert(body is not null && body.Status == "Registered", "First registration must be Registered.");
        Assert(body!.InstallationId == "device-a", "Installation id mismatch.");

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PushDeviceRegistrationDbContext>();
        var stored = await db.PushDeviceRegistrations.AsNoTracking().SingleAsync(x => x.InstallationId == "device-a");
        Assert(stored.UserId == userA, "Stored UserId must come from authenticated sub.");
        Assert(stored.PushToken == "token-a", "Push token must be persisted.");
    });

    await RunAsync("idempotent re-register returns Existing", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterPushDeviceHttpRequest("device-a", "android", "token-a"));
        var body = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse>();
        Assert(response.StatusCode == HttpStatusCode.OK && body?.Status == "Existing",
            "Same registration must return Existing.");
    });

    await RunAsync("token rotation is persisted", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterPushDeviceHttpRequest("device-a", "android", "token-b"));
        var body = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse>();
        Assert(response.StatusCode == HttpStatusCode.OK && body?.Status == "TokenRotated",
            "Changed token must return TokenRotated.");

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PushDeviceRegistrationDbContext>();
        var stored = await db.PushDeviceRegistrations.AsNoTracking().SingleAsync(x => x.InstallationId == "device-a");
        Assert(stored.PushToken == "token-b", "Rotated token must be persisted.");
    });

    await RunAsync("foreign user cannot claim existing installation", async () =>
    {
        var response = await clientB.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterPushDeviceHttpRequest("device-a", "android", "token-c"));
        Assert(response.StatusCode == HttpStatusCode.Conflict, "Foreign registration must return 409.");
    });

    await RunAsync("foreign unregister is hidden with 404", async () =>
    {
        var response = await clientB.DeleteAsync("/api/v1/push/devices/device-a");
        Assert(response.StatusCode == HttpStatusCode.NotFound, "Foreign unregister must return 404.");
    });

    await RunAsync("owner unregister deactivates registration", async () =>
    {
        var response = await clientA.DeleteAsync("/api/v1/push/devices/device-a");
        Assert(response.StatusCode == HttpStatusCode.NoContent, "Owner unregister must return 204.");

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PushDeviceRegistrationDbContext>();
        var stored = await db.PushDeviceRegistrations.AsNoTracking().SingleAsync(x => x.InstallationId == "device-a");
        Assert(!stored.IsActive && stored.DeactivatedAtUtc is not null, "Unregister must persist deactivation.");
    });

    await RunAsync("re-register after unregister returns Reactivated", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterPushDeviceHttpRequest("device-a", "android", "token-reactivated"));
        var body = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse>();
        Assert(response.StatusCode == HttpStatusCode.OK && body?.Status == "Reactivated",
            "Inactive registration must reactivate.");
    });

    await RunAsync("unsupported platform returns 400", async () =>
    {
        var response = await clientA.PostAsJsonAsync("/api/v1/push/devices",
            new RegisterPushDeviceHttpRequest("device-b", "windows", "token-x"));
        Assert(response.StatusCode == HttpStatusCode.BadRequest, "Unsupported platform must return 400.");
    });

    Console.WriteLine("AFW-BE-NOTIFICATION-PUSH-1 protected push device HTTP scenarios: PASS");
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
