using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var compositionServices = new ServiceCollection();
compositionServices.AddPaymentRequestWebhookSubscriptionReconfiguration("Data Source=:memory:");
Assert(compositionServices.Any(x => x.ServiceType == typeof(IPaymentRequestWebhookSubscriptionRegistry)),
    "Management composition must register subscription registry.");
Assert(!compositionServices.Any(x => x.ServiceType == typeof(IPaymentRequestEventTransport)),
    "Reconfiguration composition must not register or modify outbound delivery transport.");

await using var fixture = await Fixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var owner = fixture.CreateClient("integration.alpha");
var merchant = fixture.CreateClient("integration.alpha", fixture.MerchantId);
var foreign = fixture.CreateClient("integration.beta");
var missingClaims = fixture.CreateClient(null);

await RunAsync("reconfiguration requires authentication", async () =>
{
    var response = await anonymous.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated actor without integration ownership is forbidden", async () =>
{
    var response = await missingClaims.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.Forbidden, $"Expected 403, got {(int)response.StatusCode}.");
});

await RunAsync("owner rotates endpoint events and signing-key references", async () =>
{
    var response = await owner.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest());

    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var updated = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionReconfigurationResponse>();
    Assert(updated is not null, "Updated response is required.");
    Assert(updated!.EndpointUrl == "https://alpha-v2.example.test/hooks", "Endpoint rotation mismatch.");
    Assert(updated.KeyId == "alpha-key-v2", "Key id rotation mismatch.");
    Assert(updated.SecretReference == "AFW_ALPHA_WEBHOOK_SECRET_V2", "Signing-key reference rotation mismatch.");
    Assert(updated.EventTypes.SequenceEqual(new[] { "payment-request.cancelled", "payment-request.paid" }),
        "Event types must be normalized, deduplicated and sorted.");
    Assert(fixture.Registry.UpdateCalls == 1, "Owner reconfiguration must persist exactly once.");
});

await RunAsync("foreign integration is hidden with 404", async () =>
{
    var response = await foreign.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("merchant cannot modify integration-level subscription", async () =>
{
    var response = await merchant.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest());
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("raw secret field is rejected", async () =>
{
    var json = """
    {
      "endpointUrl":"https://alpha-v3.example.test/hooks",
      "keyId":"alpha-key-v3",
      "secretReference":"AFW_ALPHA_WEBHOOK_SECRET_V3",
      "eventTypes":["payment-request.paid"],
      "secret":"must-never-be-accepted"
    }
    """;
    using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    var response = await owner.PutAsync($"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}", content);
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
});

await RunAsync("invalid endpoint does not persist", async () =>
{
    var before = fixture.Registry.UpdateCalls;
    var response = await owner.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest() with { EndpointUrl = "ftp://invalid.example.test/hooks" });
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    Assert(fixture.Registry.UpdateCalls == before, "Invalid endpoint must not persist.");
});

await RunAsync("invalid signing-key reference does not persist", async () =>
{
    var before = fixture.Registry.UpdateCalls;
    var response = await owner.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}",
        fixture.ValidRequest() with { SecretReference = "raw secret value" });
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    Assert(fixture.Registry.UpdateCalls == before, "Invalid key reference must not persist.");
});

Console.WriteLine("AFW-BE-REQUEST webhook subscription reconfiguration scenarios: PASS");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class Fixture : IAsyncDisposable
{
    private Fixture(WebApplication app, InMemoryRegistry registry, Guid subscriptionId, Guid merchantId)
    {
        App = app;
        Registry = registry;
        SubscriptionId = subscriptionId;
        MerchantId = merchantId;
    }

    public WebApplication App { get; }
    public InMemoryRegistry Registry { get; }
    public Guid SubscriptionId { get; }
    public Guid MerchantId { get; }

    public ReconfigurePaymentRequestWebhookSubscriptionRequest ValidRequest() =>
        new(
            "https://alpha-v2.example.test/hooks",
            "alpha-key-v2",
            "AFW_ALPHA_WEBHOOK_SECRET_V2",
            ["payment-request.paid", "payment-request.cancelled", "payment-request.paid"]);

    public static async Task<Fixture> CreateAsync()
    {
        var subscription = PaymentRequestWebhookSubscription.Create(
            "integration.alpha",
            null,
            new Uri("https://alpha.example.test/hooks"),
            "alpha-key-v1",
            "AFW_ALPHA_WEBHOOK_SECRET_V1",
            ["payment-request.paid"],
            DateTimeOffset.UtcNow.AddMinutes(-5));

        var registry = new InMemoryRegistry(subscription);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IPaymentRequestWebhookSubscriptionRegistry>(registry);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestWebhookSubscriptionReconfigurationEndpoints();
        await app.StartAsync();

        return new Fixture(app, registry, subscription.Id, Guid.NewGuid());
    }

    public HttpClient CreateClient(string? integrationId, Guid? merchantId = null)
    {
        var client = App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
        if (!string.IsNullOrWhiteSpace(integrationId))
            client.DefaultRequestHeaders.Add("X-Test-Integration", integrationId);
        if (merchantId is not null)
            client.DefaultRequestHeaders.Add("X-Test-Merchant", merchantId.Value.ToString());
        return client;
    }

    public async ValueTask DisposeAsync() => await App.DisposeAsync();
}

sealed class InMemoryRegistry(params PaymentRequestWebhookSubscription[] subscriptions)
    : IPaymentRequestWebhookSubscriptionRegistry
{
    private readonly Dictionary<Guid, PaymentRequestWebhookSubscription> values =
        subscriptions.ToDictionary(x => x.Id);

    public int UpdateCalls { get; private set; }

    public Task<PaymentRequestWebhookSubscription?> GetAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(subscriptionId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListActiveForEventAsync(
        string eventType, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(Array.Empty<PaymentRequestWebhookSubscription>());

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListForIntegrationAsync(
        string integrationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(Array.Empty<PaymentRequestWebhookSubscription>());

    public Task AddAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task UpdateAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.ContainsKey(subscription.Id))
            throw new InvalidOperationException("Webhook subscription was not found.");
        values[subscription.Id] = subscription;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

sealed class HeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var rawUser) ||
            !Guid.TryParse(rawUser.ToString(), out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new("sub", userId.ToString()) };
        if (Request.Headers.TryGetValue("X-Test-Integration", out var integration))
            claims.Add(new Claim("integration_id", integration.ToString()));
        if (Request.Headers.TryGetValue("X-Test-Merchant", out var merchant))
            claims.Add(new Claim("merchant_id", merchant.ToString()));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
