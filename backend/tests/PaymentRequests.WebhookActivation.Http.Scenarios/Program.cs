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
    "Activation lifecycle must use the durable subscription registry.");
Assert(!compositionServices.Any(x => x.ServiceType == typeof(IPaymentRequestEventTransport)),
    "Activation lifecycle must not register or modify outbound delivery transport.");

await using var fixture = await Fixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var owner = fixture.CreateClient("integration.alpha");
var merchant = fixture.CreateClient("integration.alpha", fixture.MerchantId);
var foreign = fixture.CreateClient("integration.beta");
var missingClaims = fixture.CreateClient(null);

await RunAsync("activation controls require authentication", async () =>
{
    var response = await anonymous.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/disable", null);
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated actor without integration ownership is forbidden", async () =>
{
    var response = await missingClaims.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/disable", null);
    Assert(response.StatusCode == HttpStatusCode.Forbidden, $"Expected 403, got {(int)response.StatusCode}.");
});

await RunAsync("integration owner disables subscription without changing configuration", async () =>
{
    var before = fixture.Registry.Require(fixture.IntegrationSubscriptionId);
    var endpoint = before.Endpoint.AbsoluteUri;
    var keyId = before.KeyId;
    var secretReference = before.SecretReference;
    var eventTypes = before.EventTypes.ToArray();
    var createdAt = before.CreatedAtUtc;

    var response = await owner.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/disable", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var updated = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionActivationResponse>();
    Assert(updated is not null && updated.Status == "Disabled", "Subscription must become Disabled.");
    Assert(updated!.EndpointUrl == endpoint, "Endpoint must remain unchanged.");
    Assert(updated.KeyId == keyId, "Key id must remain unchanged.");
    Assert(updated.SecretReference == secretReference, "Secret reference must remain unchanged.");
    Assert(updated.EventTypes.SequenceEqual(eventTypes), "Event types must remain unchanged.");
    Assert(updated.CreatedAtUtc == createdAt, "Created history timestamp must remain unchanged.");
    Assert(fixture.Registry.UpdateCalls == 1 && fixture.Registry.AddCalls == 0,
        "Disable must update the same subscription without recreation.");
});

await RunAsync("integration owner re-enables same subscription", async () =>
{
    var before = fixture.Registry.Require(fixture.IntegrationSubscriptionId);
    var endpoint = before.Endpoint.AbsoluteUri;
    var keyId = before.KeyId;
    var events = before.EventTypes.ToArray();
    var createdAt = before.CreatedAtUtc;

    var response = await owner.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/enable", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");

    var updated = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionActivationResponse>();
    Assert(updated is not null && updated.Status == "Active", "Subscription must become Active.");
    Assert(updated!.EndpointUrl == endpoint && updated.KeyId == keyId, "Configuration must remain unchanged.");
    Assert(updated.EventTypes.SequenceEqual(events), "Event types must remain unchanged.");
    Assert(updated.CreatedAtUtc == createdAt, "Created history timestamp must remain unchanged.");
    Assert(fixture.Registry.UpdateCalls == 2 && fixture.Registry.AddCalls == 0,
        "Enable must update the same subscription without recreation.");
});

await RunAsync("double enable is rejected without persistence", async () =>
{
    var before = fixture.Registry.UpdateCalls;
    var response = await owner.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/enable", null);
    Assert(response.StatusCode == HttpStatusCode.Conflict, $"Expected 409, got {(int)response.StatusCode}.");
    Assert(fixture.Registry.UpdateCalls == before, "Rejected same-state transition must not persist.");
});

await RunAsync("foreign integration is hidden with 404", async () =>
{
    var response = await foreign.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/disable", null);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("merchant cannot control integration-level subscription", async () =>
{
    var response = await merchant.PostAsync($"/api/v1/webhook-subscriptions/{fixture.IntegrationSubscriptionId:D}/disable", null);
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("merchant can disable own merchant subscription", async () =>
{
    var response = await merchant.PostAsync($"/api/v1/webhook-subscriptions/{fixture.MerchantSubscriptionId:D}/disable", null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var updated = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionActivationResponse>();
    Assert(updated?.Status == "Disabled", "Merchant subscription must become Disabled.");
});

await RunAsync("disabled subscription is excluded from active routing", async () =>
{
    var active = await fixture.Registry.ListActiveForEventAsync("payment-request.paid");
    Assert(active.All(x => x.Id != fixture.MerchantSubscriptionId), "Disabled subscription must not be returned for active delivery routing.");
});

Console.WriteLine("AFW-BE-REQUEST webhook subscription activation lifecycle scenarios: PASS");

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
    private Fixture(
        WebApplication app,
        InMemoryRegistry registry,
        Guid integrationSubscriptionId,
        Guid merchantSubscriptionId,
        Guid merchantId)
    {
        App = app;
        Registry = registry;
        IntegrationSubscriptionId = integrationSubscriptionId;
        MerchantSubscriptionId = merchantSubscriptionId;
        MerchantId = merchantId;
    }

    public WebApplication App { get; }
    public InMemoryRegistry Registry { get; }
    public Guid IntegrationSubscriptionId { get; }
    public Guid MerchantSubscriptionId { get; }
    public Guid MerchantId { get; }

    public static async Task<Fixture> CreateAsync()
    {
        var merchantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.AddMinutes(-5);
        var integration = PaymentRequestWebhookSubscription.Create(
            "integration.alpha",
            null,
            new Uri("https://alpha.example.test/hooks"),
            "alpha-key-v2",
            "AFW_ALPHA_WEBHOOK_SECRET_V2",
            ["payment-request.paid", "payment-request.cancelled"],
            now);
        var merchant = PaymentRequestWebhookSubscription.Create(
            "integration.alpha",
            merchantId,
            new Uri("https://merchant-alpha.example.test/hooks"),
            "merchant-key-v1",
            "AFW_MERCHANT_ALPHA_WEBHOOK_SECRET_V1",
            ["payment-request.paid"],
            now);

        var registry = new InMemoryRegistry(integration, merchant);
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
        app.MapPaymentRequestWebhookSubscriptionActivationEndpoints();
        await app.StartAsync();

        return new Fixture(app, registry, integration.Id, merchant.Id, merchantId);
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

    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public PaymentRequestWebhookSubscription Require(Guid id) => values[id];

    public Task<PaymentRequestWebhookSubscription?> GetAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(subscriptionId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListActiveForEventAsync(
        string eventType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = PaymentRequestWebhookSubscription.NormalizeEventType(eventType);
        return Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(
            values.Values.Where(x => x.Status == PaymentRequestWebhookSubscriptionStatus.Active && x.SubscribesTo(normalized)).ToArray());
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListForIntegrationAsync(
        string integrationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = PaymentRequestWebhookSubscription.NormalizeIntegrationId(integrationId);
        return Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(
            values.Values.Where(x => x.IntegrationId == normalized).ToArray());
    }

    public Task AddAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(subscription.Id, subscription);
        AddCalls++;
        return Task.CompletedTask;
    }

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
