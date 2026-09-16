using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.PushNotifications.Application;
using AfriWallet.PushNotifications.Domain;
using IdentityService.Api.PushNotifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await PushHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var userClient = CreateClient(fixture.App, fixture.UserId);
var otherClient = CreateClient(fixture.App, fixture.OtherUserId);

await RunAsync("push device registration requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync("/api/v1/push/devices/", new RegisterPushDeviceHttpRequest("phone-1", "android", "token-1"));
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated user registers device from JWT sub", async () =>
{
    var response = await userClient.PostAsJsonAsync("/api/v1/push/devices/", new RegisterPushDeviceHttpRequest(" phone-1 ", "android", "token-1"));
    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse>();
    Assert(body?.DeviceId == "phone-1", "Device id must be normalized.");
    Assert(fixture.Repository.Items.Single().UserId == fixture.UserId, "User id must come from authenticated sub.");
});

await RunAsync("same user refreshes existing device", async () =>
{
    var response = await userClient.PostAsJsonAsync("/api/v1/push/devices/", new RegisterPushDeviceHttpRequest("phone-1", "ios", "token-2"));
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    Assert(fixture.Repository.Items.Count == 1, "Refresh must not create a duplicate device registration.");
});

await RunAsync("list is scoped to authenticated user", async () =>
{
    var response = await userClient.GetAsync("/api/v1/push/devices/");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var devices = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse[]>();
    Assert(devices is { Length: 1 } && devices[0].DeviceId == "phone-1", "Owner must see own device.");

    var other = await otherClient.GetAsync("/api/v1/push/devices/");
    var otherDevices = await other.Content.ReadFromJsonAsync<PushDeviceHttpResponse[]>();
    Assert(otherDevices is { Length: 0 }, "Other user must not see foreign devices.");
});

await RunAsync("other user cannot revoke foreign device", async () =>
{
    var response = await otherClient.DeleteAsync("/api/v1/push/devices/phone-1");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
    Assert(fixture.Repository.Items.Single().Status == PushDeviceStatus.Active, "Foreign revoke must not change device.");
});

await RunAsync("owner can revoke own device", async () =>
{
    var response = await userClient.DeleteAsync("/api/v1/push/devices/phone-1");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<PushDeviceHttpResponse>();
    Assert(body?.Status == "revoked", "Device must be revoked.");
    Assert(fixture.Repository.Items.Single().Status == PushDeviceStatus.Revoked, "Repository must persist revoked state.");
});

await RunAsync("invalid platform returns 400", async () =>
{
    var response = await userClient.PostAsJsonAsync("/api/v1/push/devices/", new RegisterPushDeviceHttpRequest("phone-2", "windows", "token-x"));
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

var services = new ServiceCollection();
services.AddPushDeviceRegistration("Data Source=push-http-composition.db");
Assert(services.Any(x => x.ServiceType == typeof(IPushDeviceRepository) && x.ImplementationType?.Name == "EfPushDeviceRepository"), "Composition must wire EF repository.");
Assert(services.Any(x => x.ServiceType == typeof(PushDeviceRegistrationService)), "Composition must wire application service.");

Console.WriteLine("AFW-BE-PUSH-1 protected push device HTTP scenarios: PASS");

static HttpClient CreateClient(WebApplication app, Guid userId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
    return client;
}

static async Task RunAsync(string name, Func<Task> action)
{
    await action();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class PushHttpFixture : IAsyncDisposable
{
    private PushHttpFixture(WebApplication app, InMemoryPushDeviceRepository repository, Guid userId, Guid otherUserId)
    {
        App = app;
        Repository = repository;
        UserId = userId;
        OtherUserId = otherUserId;
    }

    public WebApplication App { get; }
    public InMemoryPushDeviceRepository Repository { get; }
    public Guid UserId { get; }
    public Guid OtherUserId { get; }

    public static async Task<PushHttpFixture> CreateAsync()
    {
        var repository = new InMemoryPushDeviceRepository();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IPushDeviceRepository>(repository);
        builder.Services.AddScoped<PushDeviceRegistrationService>();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPushDeviceEndpoints();
        await app.StartAsync();
        return new PushHttpFixture(app, repository, Guid.NewGuid(), Guid.NewGuid());
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class InMemoryPushDeviceRepository : IPushDeviceRepository
{
    public List<PushDeviceRegistration> Items { get; } = [];

    public Task<PushDeviceRegistration?> FindByUserAndDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Items.SingleOrDefault(x => x.UserId == userId && x.DeviceId == deviceId));
    }

    public Task AddAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Items.Add(registration);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PushDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PushDeviceRegistration>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PushDeviceRegistration>>(Items.Where(x => x.UserId == userId).ToArray());
    }
}

sealed class HeaderAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var raw) || !Guid.TryParse(raw.ToString(), out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
