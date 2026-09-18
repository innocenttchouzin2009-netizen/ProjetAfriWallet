using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.Webhooks.Application;
using AfriWallet.Webhooks.Persistence;
using IdentityService.Api.Webhooks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await RunAsync("composition wires SQLite repository without outbound delivery engine", () =>
{
    var services = new ServiceCollection();
    services.AddWebhookManagement("Data Source=:memory:");

    Assert(
        services.Any(descriptor =>
            descriptor.ServiceType == typeof(IWebhookSubscriptionRepository) &&
            descriptor.ImplementationType == typeof(EfWebhookSubscriptionRepository)),
        "Composition must wire EF webhook subscription repository.");

    Assert(
        services.Any(descriptor =>
            descriptor.ServiceType == typeof(WebhookManagementService)),
        "Composition must wire WebhookManagementService.");

    Assert(
        !services.Any(descriptor =>
            string.Equals(
                descriptor.ServiceType.FullName,
                "AfriWallet.PaymentRequests.Application.IPaymentRequestEventTransport",
                StringComparison.Ordinal)),
        "Webhook management composition must not register outbound delivery transport.");

    return Task.CompletedTask;
});

await using var fixture = await WebhookManagementHttpFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var ownerClient = CreateClient(fixture.App, fixture.OwnerId);
var otherClient = CreateClient(fixture.App, fixture.OtherOwnerId);

await RunAsync("registration requires authentication", async () =>
{
    var response = await anonymous.PostAsJsonAsync(
        "/api/v1/webhook-subscriptions",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

Guid subscriptionId = Guid.Empty;
await RunAsync("authenticated owner can register subscription", async () =>
{
    var response = await ownerClient.PostAsJsonAsync(
        "/api/v1/webhook-subscriptions",
        fixture.ValidRequest());

    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<WebhookSubscriptionResponse>();
    Assert(body is not null, "Created subscription response is required.");
    subscriptionId = body!.Id;
    Assert(subscriptionId != Guid.Empty, "Subscription id must be generated.");
    Assert(body.Status == AfriWallet.Webhooks.Domain.WebhookSubscriptionStatus.Active, "New subscription must be active.");
    Assert(body.EventTypes.SequenceEqual(["payment_request.paid", "payment_request.pending"]), "Event types must be normalized.");
});

await RunAsync("client cannot inject owner id", async () =>
{
    var payload = new
    {
        ownerId = fixture.OtherOwnerId,
        endpoint = "https://merchant.example/webhooks",
        eventTypes = new[] { "payment_request.paid" },
        signingKeyReference = "vault-key-1"
    };

    var response = await ownerClient.PostAsJsonAsync("/api/v1/webhook-subscriptions", payload);
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("owner can list own subscriptions", async () =>
{
    var response = await ownerClient.GetAsync("/api/v1/webhook-subscriptions");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<WebhookSubscriptionResponse[]>();
    Assert(body is { Length: 1 } && body[0].Id == subscriptionId, "Owner must see exactly the owned subscription.");
});

await RunAsync("foreign owner cannot read subscription", async () =>
{
    var response = await otherClient.GetAsync($"/api/v1/webhook-subscriptions/{subscriptionId:D}");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("owner can disable subscription", async () =>
{
    var response = await ownerClient.PostAsync(
        $"/api/v1/webhook-subscriptions/{subscriptionId:D}/disable",
        content: null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<WebhookSubscriptionResponse>();
    Assert(body?.Status == AfriWallet.Webhooks.Domain.WebhookSubscriptionStatus.Disabled, "Subscription must be disabled.");
});

await RunAsync("foreign owner cannot enable subscription", async () =>
{
    var response = await otherClient.PostAsync(
        $"/api/v1/webhook-subscriptions/{subscriptionId:D}/enable",
        content: null);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("owner can re-enable subscription", async () =>
{
    var response = await ownerClient.PostAsync(
        $"/api/v1/webhook-subscriptions/{subscriptionId:D}/enable",
        content: null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var body = await response.Content.ReadFromJsonAsync<WebhookSubscriptionResponse>();
    Assert(body?.Status == AfriWallet.Webhooks.Domain.WebhookSubscriptionStatus.Active, "Subscription must be active.");
});

await RunAsync("invalid endpoint is rejected", async () =>
{
    var response = await ownerClient.PostAsJsonAsync(
        "/api/v1/webhook-subscriptions",
        fixture.ValidRequest() with { Endpoint = "http://merchant.example/webhooks" });
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

Console.WriteLine("AFW-BE-WEBHOOK-MGMT-1 protected HTTP and composition scenarios: PASS");

static HttpClient CreateClient(WebApplication app, Guid ownerId)
{
    var client = app.GetTestClient();
    client.DefaultRequestHeaders.Add("X-Test-User", ownerId.ToString());
    return client;
}

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class WebhookManagementHttpFixture : IAsyncDisposable
{
    private readonly string databasePath;

    private WebhookManagementHttpFixture(
        WebApplication app,
        Guid ownerId,
        Guid otherOwnerId,
        string databasePath)
    {
        App = app;
        OwnerId = ownerId;
        OtherOwnerId = otherOwnerId;
        this.databasePath = databasePath;
    }

    public WebApplication App { get; }
    public Guid OwnerId { get; }
    public Guid OtherOwnerId { get; }

    public RegisterWebhookSubscriptionRequest ValidRequest() =>
        new(
            "https://merchant.example/webhooks",
            ["payment_request.pending", "PAYMENT_REQUEST.PAID"],
            "vault-key-1");

    public static async Task<WebhookManagementHttpFixture> CreateAsync()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"afw-webhook-mgmt-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddWebhookManagement(connectionString);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapWebhookManagementEndpoints();

        await app.StartAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WebhookManagementDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new WebhookManagementHttpFixture(
            app,
            Guid.NewGuid(),
            Guid.NewGuid(),
            databasePath);
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        if (File.Exists(databasePath))
            File.Delete(databasePath);
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
            !Guid.TryParse(raw.ToString(), out var ownerId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim("sub", ownerId.ToString())],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
