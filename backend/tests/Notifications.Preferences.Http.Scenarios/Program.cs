using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using IdentityService.Api.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await NotificationPreferenceHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var ownerClient = CreateClient(fixture.App, fixture.OwnerId);

await RunAsync("notification preferences require authentication", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/notification-preferences");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated GET returns only caller preferences", async () =>
{
    var response = await ownerClient.GetAsync("/api/v1/notification-preferences");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var values = await response.Content.ReadFromJsonAsync<NotificationPreferenceResponse[]>();
    Assert(values is { Length: 2 }, "Expected both notification channels.");
    Assert(values!.Any(x => x.Channel == "in-app" && x.IsEnabled), "In-app default must be enabled.");
    Assert(values.Any(x => x.Channel == "push" && x.IsEnabled), "Push default must be enabled.");
});

await RunAsync("PUT derives user exclusively from sub and ignores injected userId", async () =>
{
    var payload = new { isEnabled = false, userId = fixture.OtherUserId };
    var response = await ownerClient.PutAsJsonAsync("/api/v1/notification-preferences/push", payload);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var result = await response.Content.ReadFromJsonAsync<NotificationPreferenceUpdateResponse>();
    Assert(result?.Preference.Channel == "push" && result.Preference.IsEnabled == false, "Caller push preference must be disabled.");

    await using var scope = fixture.App.Services.CreateAsyncScope();
    var repository = scope.ServiceProvider.GetRequiredService<INotificationPreferenceRepository>();
    var owner = await repository.GetAsync(fixture.OwnerId, NotificationChannel.Push);
    var other = await repository.GetAsync(fixture.OtherUserId, NotificationChannel.Push);
    Assert(owner is not null && !owner.IsEnabled, "Authenticated caller preference must change.");
    Assert(other is not null && other.IsEnabled, "Foreign user preference must remain unchanged.");
});

await RunAsync("locked in-app preference cannot be disabled", async () =>
{
    var response = await ownerClient.PutAsJsonAsync(
        "/api/v1/notification-preferences/in-app",
        new UpdateNotificationPreferenceRequest(false));
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    var error = await response.Content.ReadFromJsonAsync<NotificationPreferenceErrorResponse>();
    Assert(error?.Code == NotificationPreferenceErrorCode.Conflict, "Expected notification preference conflict.");
});

await RunAsync("invalid notification channel returns 400", async () =>
{
    var response = await ownerClient.PutAsJsonAsync(
        "/api/v1/notification-preferences/email",
        new UpdateNotificationPreferenceRequest(false));
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("foreign-user-scoped preference route does not exist", async () =>
{
    var response = await ownerClient.GetAsync($"/api/v1/notification-preferences/{fixture.OtherUserId}/push");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

Console.WriteLine("AFW-BE-NOTIFICATION-PREFERENCES-1 protected HTTP and composition scenarios: PASS");

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

sealed class NotificationPreferenceHttpFixture : IAsyncDisposable
{
    private NotificationPreferenceHttpFixture(WebApplication app, string databasePath, Guid ownerId, Guid otherUserId)
    {
        App = app;
        DatabasePath = databasePath;
        OwnerId = ownerId;
        OtherUserId = otherUserId;
    }

    public WebApplication App { get; }
    public string DatabasePath { get; }
    public Guid OwnerId { get; }
    public Guid OtherUserId { get; }

    public static async Task<NotificationPreferenceHttpFixture> CreateAsync()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"afw-notification-preferences-http-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddNotificationPreferences(connectionString);

        if (!builder.Services.Any(descriptor =>
                descriptor.ServiceType == typeof(INotificationPreferenceRepository) &&
                descriptor.ImplementationType == typeof(EfNotificationPreferenceRepository)))
        {
            throw new InvalidOperationException("Notification preferences composition must wire EfNotificationPreferenceRepository.");
        }

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapNotificationPreferenceEndpoints();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationPreferenceDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            var service = scope.ServiceProvider.GetRequiredService<NotificationPreferenceApplicationService>();
            await service.GetAsync(otherUserId);
        }

        await app.StartAsync();
        return new NotificationPreferenceHttpFixture(app, databasePath, ownerId, otherUserId);
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        if (File.Exists(DatabasePath)) File.Delete(DatabasePath);
    }
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
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
