using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AfriWallet.PaymentRequests.Webhooks;
using IdentityService.Api.PaymentRequests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

await using var fixture = await WebhookManagementFixture.CreateAsync();

var anonymous = fixture.App.GetTestClient();
var integrationClient = fixture.CreateClient("integration.alpha");
var merchantClient = fixture.CreateClient("integration.alpha", fixture.MerchantId);
var foreignClient = fixture.CreateClient("integration.beta");
var claimsMissingClient = fixture.CreateClient(null);

await RunAsync("management API requires authentication", async () =>
{
    var response = await anonymous.GetAsync("/api/v1/webhook-subscriptions");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}.");
});

await RunAsync("authenticated actor without integration claim is forbidden", async () =>
{
    var response = await claimsMissingClient.GetAsync("/api/v1/webhook-subscriptions");
    Assert(response.StatusCode == HttpStatusCode.Forbidden, $"Expected 403, got {(int)response.StatusCode}.");
});

Guid integrationSubscriptionId = Guid.Empty;
await RunAsync("integration can create subscription without raw secret", async () =>
{
    var response = await integrationClient.PostAsJsonAsync(
        "/api/v1/webhook-subscriptions",
        new CreatePaymentRequestWebhookSubscriptionRequest(
            "https://alpha.example.test/hooks",
            "alpha-key-v1",
            "AFW_ALPHA_WEBHOOK_SECRET_V1",
            ["payment-request.paid", "payment-request.cancelled"]));

    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    var created = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse>();
    Assert(created is not null, "Created subscription response is required.");
    integrationSubscriptionId = created!.Id;
    Assert(created.IntegrationId == "integration.alpha", "Integration ownership must come from authenticated claims.");
    Assert(created.MerchantId is null, "Integration-level subscription must not invent merchant ownership.");
    Assert(created.SecretReference == "AFW_ALPHA_WEBHOOK_SECRET_V1", "Secret reference must be returned by reference only.");
    var payload = await response.Content.ReadAsStringAsync();
    Assert(!payload.Contains("\"secret\"", StringComparison.OrdinalIgnoreCase), "Raw secret property must never be returned.");
});

await RunAsync("raw secret field is explicitly rejected", async () =>
{
    var json = """
    {
      "endpointUrl":"https://raw-secret.example.test/hooks",
      "keyId":"raw-secret-key",
      "secretReference":"AFW_RAW_SECRET_REFERENCE",
      "eventTypes":["payment-request.paid"],
      "secret":"this-must-never-be-accepted"
    }
    """;
    using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    var response = await integrationClient.PostAsync("/api/v1/webhook-subscriptions", content);
    Assert(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}.");
    Assert(fixture.Registry.Count == 1, "Rejected raw-secret request must not create a subscription.");
});

Guid merchantSubscriptionId = Guid.Empty;
await RunAsync("merchant-scoped actor creates merchant-owned subscription", async () =>
{
    var response = await merchantClient.PostAsJsonAsync(
        "/api/v1/webhook-subscriptions",
        new CreatePaymentRequestWebhookSubscriptionRequest(
            "https://merchant.example.test/hooks",
            "merchant-key-v1",
            "AFW_MERCHANT_WEBHOOK_SECRET_V1",
            ["payment-request.paid"]));

    Assert(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}.");
    var created = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse>();
    Assert(created?.MerchantId == fixture.MerchantId, "Merchant ownership must be derived from authenticated claims.");
    merchantSubscriptionId = created!.Id;
});

await RunAsync("integration lists all subscriptions in its integration", async () =>
{
    var response = await integrationClient.GetAsync("/api/v1/webhook-subscriptions");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var list = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse[]>();
    Assert(list?.Length == 2, "Integration actor must see integration-level and merchant-scoped subscriptions.");
});

await RunAsync("merchant lists only its merchant subscriptions", async () =>
{
    var response = await merchantClient.GetAsync("/api/v1/webhook-subscriptions");
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var list = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse[]>();
    Assert(list?.Length == 1 && list[0].Id == merchantSubscriptionId, "Merchant actor must only see its own merchant subscriptions.");
});

await RunAsync("foreign integration is hidden with 404", async () =>
{
    var response = await foreignClient.GetAsync($"/api/v1/webhook-subscriptions/{integrationSubscriptionId:D}");
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("integration updates endpoint events and key configuration by reference", async () =>
{
    var response = await integrationClient.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{integrationSubscriptionId:D}",
        new UpdatePaymentRequestWebhookSubscriptionRequest(
            "https://alpha-v2.example.test/hooks",
            "alpha-key-v2",
            "AFW_ALPHA_WEBHOOK_SECRET_V2",
            ["payment-request.declined", "payment-request.paid"]));

    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var updated = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse>();
    Assert(updated?.EndpointUrl == "https://alpha-v2.example.test/hooks", "Endpoint update mismatch.");
    Assert(updated?.KeyId == "alpha-key-v2", "Key id update mismatch.");
    Assert(updated?.SecretReference == "AFW_ALPHA_WEBHOOK_SECRET_V2", "Secret reference update mismatch.");
    Assert(updated?.EventTypes.SequenceEqual(new[] { "payment-request.declined", "payment-request.paid" }) == true,
        "Event set must be normalized and updated.");
});

await RunAsync("merchant cannot modify integration-level subscription", async () =>
{
    var response = await merchantClient.PutAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{integrationSubscriptionId:D}",
        new UpdatePaymentRequestWebhookSubscriptionRequest(
            "https://forbidden.example.test/hooks",
            "forbidden-key",
            "AFW_FORBIDDEN_SECRET_REFERENCE",
            ["payment-request.paid"]));
    Assert(response.StatusCode == HttpStatusCode.NotFound, $"Expected 404, got {(int)response.StatusCode}.");
});

await RunAsync("owner can disable subscription without deleting it", async () =>
{
    var response = await integrationClient.PostAsync(
        $"/api/v1/webhook-subscriptions/{integrationSubscriptionId:D}/disable",
        content: null);
    Assert(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}.");
    var disabled = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse>();
    Assert(disabled?.Status == "Disabled", "Subscription must become Disabled.");

    var get = await integrationClient.GetAsync($"/api/v1/webhook-subscriptions/{integrationSubscriptionId:D}");
    Assert(get.StatusCode == HttpStatusCode.OK, "Disabled subscription must remain readable.");
});

Console.WriteLine("AFW-BE-REQUEST Commit 10 protected webhook subscription management API scenarios: PASS");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class WebhookManagementFixture : IAsyncDisposable
{
    private WebhookManagementFixture(WebApplication app, InMemoryWebhookSubscriptionRegistry registry, Guid merchantId)
    {
        App = app;
        Registry = registry;
        MerchantId = merchantId;
    }

    public WebApplication App { get; }
    public InMemoryWebhookSubscriptionRegistry Registry { get; }
    public Guid MerchantId { get; }

    public static async Task<WebhookManagementFixture> CreateAsync()
    {
        var registry = new InMemoryWebhookSubscriptionRegistry();
        var merchantId = Guid.NewGuid();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderWebhookManagementAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IPaymentRequestWebhookSubscriptionRegistry>(registry);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestWebhookSubscriptionManagementEndpoints();
        await app.StartAsync();

        return new WebhookManagementFixture(app, registry, merchantId);
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

sealed class InMemoryWebhookSubscriptionRegistry : IPaymentRequestWebhookSubscriptionRegistry
{
    private readonly Dictionary<Guid, PaymentRequestWebhookSubscription> values = new();
    public int Count => values.Count;

    public Task<PaymentRequestWebhookSubscription?> GetAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(subscriptionId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListActiveForEventAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = PaymentRequestWebhookSubscription.NormalizeEventType(eventType);
        return Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(
            values.Values.Where(x => x.Status == PaymentRequestWebhookSubscriptionStatus.Active && x.SubscribesTo(normalized)).ToArray());
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListForIntegrationAsync(
        string integrationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = PaymentRequestWebhookSubscription.NormalizeIntegrationId(integrationId);
        return Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(
            values.Values.Where(x => x.IntegrationId == normalized).OrderBy(x => x.Id).ToArray());
    }

    public Task AddAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (values.Values.Any(x =>
                x.IntegrationId == subscription.IntegrationId &&
                x.Endpoint.AbsoluteUri == subscription.Endpoint.AbsoluteUri))
            throw new InvalidOperationException("Duplicate webhook subscription.");
        values.Add(subscription.Id, subscription);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.ContainsKey(subscription.Id))
            throw new InvalidOperationException("Webhook subscription was not found.");
        values[subscription.Id] = subscription;
        return Task.CompletedTask;
    }
}

sealed class HeaderWebhookManagementAuthHandler(
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
