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

await using var fixture = await OperationsFixture.CreateAsync();
var anonymous = fixture.App.GetTestClient();
var owner = fixture.CreateClient("integration.alpha");
var foreign = fixture.CreateClient("integration.beta");

await RunAsync("operations require authentication", async () =>
{
    var response = await anonymous.GetAsync($"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/health");
    Assert(response.StatusCode == HttpStatusCode.Unauthorized, "Expected 401.");
});

await RunAsync("foreign integration is hidden", async () =>
{
    var response = await foreign.GetAsync($"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/health");
    Assert(response.StatusCode == HttpStatusCode.NotFound, "Expected 404.");
});

await RunAsync("raw secret field is rejected during rotation", async () =>
{
    var json = $$"""
    {
      "keyId":"key-v2",
      "secretReference":"AFW_WEBHOOK_SECRET_V2",
      "secret":"raw-secret-must-not-be-accepted"
    }
    """;
    using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    var response = await owner.PostAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/rotate-credentials",
        content);
    Assert(response.StatusCode == HttpStatusCode.BadRequest, "Expected 400.");
});

await RunAsync("credential references rotate without returning raw secret", async () =>
{
    var response = await owner.PostAsJsonAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/rotate-credentials",
        new RotatePaymentRequestWebhookSubscriptionRequest("key-v2", "AFW_WEBHOOK_SECRET_V2"));
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var payload = await response.Content.ReadAsStringAsync();
    Assert(payload.Contains("key-v2", StringComparison.Ordinal), "New key id must be visible.");
    Assert(payload.Contains("AFW_WEBHOOK_SECRET_V2", StringComparison.Ordinal), "Secret reference must be visible.");
    Assert(!payload.Contains(fixture.RawSecret, StringComparison.Ordinal), "Raw secret must never be returned.");
});

await RunAsync("disabled subscription can be re-enabled", async () =>
{
    fixture.Subscription.Disable(DateTimeOffset.UtcNow);
    var response = await owner.PostAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/enable", null);
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var result = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookSubscriptionResponse>();
    Assert(result?.Status == "Active", "Subscription must be active.");
});

await RunAsync("connectivity test records safe audit metadata", async () =>
{
    var response = await owner.PostAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/test-connectivity", null);
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var result = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookConnectivityTestResponse>();
    Assert(result?.Succeeded == true && result.HttpStatusCode == 204, "Connectivity result mismatch.");

    var auditResponse = await owner.GetAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/audit");
    var auditPayload = await auditResponse.Content.ReadAsStringAsync();
    Assert(auditResponse.StatusCode == HttpStatusCode.OK, "Audit endpoint must succeed.");
    Assert(auditPayload.Contains("ConnectivityTested", StringComparison.Ordinal), "Connectivity audit entry missing.");
    Assert(auditPayload.Contains("CredentialsRotated", StringComparison.Ordinal), "Rotation audit entry missing.");
    Assert(auditPayload.Contains("Enabled", StringComparison.Ordinal), "Enable audit entry missing.");
    Assert(!auditPayload.Contains(fixture.RawSecret, StringComparison.Ordinal), "Audit must never expose raw secret.");
});

await RunAsync("health reflects latest successful connectivity test", async () =>
{
    var response = await owner.GetAsync(
        $"/api/v1/webhook-subscriptions/{fixture.SubscriptionId:D}/health");
    Assert(response.StatusCode == HttpStatusCode.OK, "Expected 200.");
    var health = await response.Content.ReadFromJsonAsync<PaymentRequestWebhookDeliveryHealthResponse>();
    Assert(health?.Health == "Healthy", "Expected Healthy.");
    Assert(health.LastConnectivitySucceeded == true, "Expected successful last probe.");
});

Console.WriteLine("AFW-BE-REQUEST Commit 11 webhook subscription operations scenarios: PASS");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class OperationsFixture : IAsyncDisposable
{
    private OperationsFixture(
        WebApplication app,
        PaymentRequestWebhookSubscription subscription,
        string rawSecret)
    {
        App = app;
        Subscription = subscription;
        RawSecret = rawSecret;
    }

    public WebApplication App { get; }
    public PaymentRequestWebhookSubscription Subscription { get; }
    public Guid SubscriptionId => Subscription.Id;
    public string RawSecret { get; }

    public static async Task<OperationsFixture> CreateAsync()
    {
        const string rawSecret = "this-is-a-test-secret-value-that-must-never-be-returned";
        var subscription = PaymentRequestWebhookSubscription.Create(
            "integration.alpha",
            null,
            new Uri("https://receiver.example.test/hooks"),
            "key-v1",
            "AFW_WEBHOOK_SECRET_V1",
            ["payment-request.paid"],
            DateTimeOffset.UtcNow.AddMinutes(-5));

        var registry = new InMemoryRegistry(subscription);
        var audit = new InMemoryAuditStore();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IPaymentRequestWebhookSubscriptionRegistry>(registry);
        builder.Services.AddSingleton<IPaymentRequestWebhookSubscriptionAuditStore>(audit);
        builder.Services.AddSingleton<IPaymentRequestWebhookDeliveryAttemptStore>(
            new InMemoryDeliveryAttemptStore());
        builder.Services.AddSingleton<IPaymentRequestWebhookSigningSecretResolver>(
            new FixedSecretResolver(rawSecret));
        builder.Services.AddSingleton<IPaymentRequestWebhookConnectivityProbe>(
            new FixedProbe());

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentRequestWebhookSubscriptionOperationsEndpoints();
        await app.StartAsync();
        return new OperationsFixture(app, subscription, rawSecret);
    }

    public HttpClient CreateClient(string integrationId)
    {
        var client = App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Integration", integrationId);
        return client;
    }

    public ValueTask DisposeAsync() => App.DisposeAsync();
}

sealed class InMemoryRegistry(PaymentRequestWebhookSubscription initial)
    : IPaymentRequestWebhookSubscriptionRegistry
{
    private readonly Dictionary<Guid, PaymentRequestWebhookSubscription> values = new()
    {
        [initial.Id] = initial
    };

    public Task<PaymentRequestWebhookSubscription?> GetAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(subscriptionId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListActiveForEventAsync(
        string eventType, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(
            values.Values.Where(x => x.Status == PaymentRequestWebhookSubscriptionStatus.Active && x.SubscribesTo(eventType)).ToArray());

    public Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListForIntegrationAsync(
        string integrationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscription>>(
            values.Values.Where(x => x.IntegrationId == PaymentRequestWebhookSubscription.NormalizeIntegrationId(integrationId)).ToArray());

    public Task AddAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        values.Add(subscription.Id, subscription);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentRequestWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        values[subscription.Id] = subscription;
        return Task.CompletedTask;
    }
}

sealed class InMemoryAuditStore : IPaymentRequestWebhookSubscriptionAuditStore
{
    private readonly List<PaymentRequestWebhookSubscriptionAuditEntry> values = [];

    public Task AppendAsync(PaymentRequestWebhookSubscriptionAuditEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PaymentRequestWebhookSubscriptionAuditEntry>> ListAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PaymentRequestWebhookSubscriptionAuditEntry>>(
            values.Where(x => x.SubscriptionId == subscriptionId)
                .OrderByDescending(x => x.OccurredAtUtc).ToArray());
    }
}

sealed class FixedSecretResolver(string secret) : IPaymentRequestWebhookSigningSecretResolver
{
    public Task<string> ResolveAsync(string secretReference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(secret);
    }
}

sealed class FixedProbe : IPaymentRequestWebhookConnectivityProbe
{
    public Task<PaymentRequestWebhookConnectivityProbeResult> ProbeAsync(
        Uri endpoint, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentRequestWebhookConnectivityProbeResult(
            true, 204, "Endpoint responded.", DateTimeOffset.UtcNow));
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

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}


sealed class InMemoryDeliveryAttemptStore : IPaymentRequestWebhookDeliveryAttemptStore
{
    private readonly List<PaymentRequestWebhookDeliveryAttempt> values = [];

    public Task AppendAsync(
        PaymentRequestWebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(attempt);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PaymentRequestWebhookDeliveryAttempt>> ListAsync(
        Guid subscriptionId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PaymentRequestWebhookDeliveryAttempt>>(
            values.Where(x => x.SubscriptionId == subscriptionId)
                .OrderByDescending(x => x.CompletedAtUtc)
                .Take(limit)
                .ToArray());
    }

    public Task<PaymentRequestWebhookDeliveryReliabilityMetrics> GetMetricsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var attempts = values.Where(x => x.SubscriptionId == subscriptionId).ToArray();
        if (attempts.Length == 0)
            return Task.FromResult(PaymentRequestWebhookDeliveryReliabilityMetrics.Empty(subscriptionId));

        var successful = attempts.Count(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.Success);
        var transient = attempts.Count(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure);
        var permanent = attempts.Count(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure);
        var failures = transient + permanent;
        var lastSuccess = attempts
            .Where(x => x.Outcome == PaymentRequestWebhookDeliveryAttemptOutcome.Success)
            .OrderByDescending(x => x.CompletedAtUtc)
            .FirstOrDefault();

        return Task.FromResult(new PaymentRequestWebhookDeliveryReliabilityMetrics(
            subscriptionId,
            attempts.Length,
            successful,
            transient,
            permanent,
            (decimal)failures / attempts.Length,
            attempts.Max(x => x.CompletedAtUtc),
            lastSuccess?.CompletedAtUtc,
            attempts.Average(x => (double)x.LatencyMilliseconds)));
    }
}
