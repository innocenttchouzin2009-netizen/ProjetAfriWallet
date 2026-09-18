using System.Net;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;
using AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-webhook-registry-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<PaymentRequestWebhookSubscriptionDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

try
{
    await using var db = new PaymentRequestWebhookSubscriptionDbContext(options);
    await db.Database.EnsureCreatedAsync();
    var registry = new EfPaymentRequestWebhookSubscriptionRegistry(db);
    var now = new DateTimeOffset(2026, 9, 18, 15, 0, 0, TimeSpan.Zero);
    var merchantId = Guid.NewGuid();

    var one = PaymentRequestWebhookSubscription.Create(
        "integration.alpha",
        merchantId,
        new Uri("https://alpha.example.test/hooks"),
        "alpha-key",
        "AFW_TEST_WEBHOOK_ALPHA_SECRET",
        ["payment-request.paid"],
        now);
    var two = PaymentRequestWebhookSubscription.Create(
        "integration.beta",
        null,
        new Uri("https://beta.example.test/hooks"),
        "beta-key",
        "AFW_TEST_WEBHOOK_BETA_SECRET",
        ["payment-request.paid", "payment-request.cancelled"],
        now);
    var disabled = PaymentRequestWebhookSubscription.Create(
        "integration.disabled",
        null,
        new Uri("https://disabled.example.test/hooks"),
        "disabled-key",
        "AFW_TEST_WEBHOOK_DISABLED_SECRET",
        ["payment-request.paid"],
        now);
    disabled.Disable(now.AddSeconds(1));

    await registry.AddAsync(one);
    await registry.AddAsync(two);
    await registry.AddAsync(disabled);

    var paid = await registry.ListActiveForEventAsync(" PAYMENT-REQUEST.PAID ");
    Assert(paid.Count == 2, "Paid event must resolve two active destinations.");
    Assert(paid.All(x => x.Status == PaymentRequestWebhookSubscriptionStatus.Active), "Disabled destinations must not resolve.");
    Assert(paid.Single(x => x.IntegrationId == "integration.alpha").MerchantId == merchantId, "Merchant ownership must round-trip.");

    var cancelled = await registry.ListActiveForEventAsync("payment-request.cancelled");
    Assert(cancelled.Count == 1 && cancelled[0].IntegrationId == "integration.beta", "Event subscriptions must filter destinations.");

    var stored = await db.Subscriptions.AsNoTracking().SingleAsync(x => x.Id == one.Id);
    Assert(stored.SecretReference == "AFW_TEST_WEBHOOK_ALPHA_SECRET", "Only secret reference may be persisted.");
    Assert(!stored.SecretReference.Contains("0123456789abcdef", StringComparison.Ordinal), "Raw secret must never be persisted.");

    Environment.SetEnvironmentVariable("AFW_TEST_WEBHOOK_ALPHA_SECRET", "0123456789abcdef0123456789abcdef");
    Environment.SetEnvironmentVariable("AFW_TEST_WEBHOOK_BETA_SECRET", "abcdef0123456789abcdef0123456789");

    var handler = new RecordingHandler();
    var transport = new RegistryBackedHttpPaymentRequestEventTransport(
        new HttpClient(handler),
        registry,
        new EnvironmentPaymentRequestWebhookSigningSecretResolver(),
        new FixedTimeProvider(now.AddMinutes(1)));

    var dispatch = new PaymentRequestEventDispatch(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "payment-request.paid",
        now,
        """{"status":"Paid"}""");

    await transport.DispatchAsync(dispatch);
    Assert(handler.Deliveries.Count == 2, "Paid event must fan out to both active matching endpoints.");
    Assert(handler.Deliveries.Select(x => x.Uri.Host).OrderBy(x => x).SequenceEqual(
        new[] { "alpha.example.test", "beta.example.test" }), "Fan-out endpoint set mismatch.");
    Assert(handler.Deliveries.Single(x => x.Uri.Host == "alpha.example.test").KeyId == "alpha-key", "Alpha key id mismatch.");
    Assert(handler.Deliveries.Single(x => x.Uri.Host == "beta.example.test").KeyId == "beta-key", "Beta key id mismatch.");
    Assert(handler.Deliveries.All(x => x.EventId == dispatch.EventId.ToString("D")), "Event id must remain stable across destinations.");

    one.Disable(now.AddMinutes(2));
    await registry.UpdateAsync(one);
    handler.Reset();
    await transport.DispatchAsync(dispatch);
    Assert(handler.Deliveries.Count == 1 && handler.Deliveries[0].Uri.Host == "beta.example.test",
        "Disabled subscription must stop receiving events.");

    handler.StatusByHost["beta.example.test"] = HttpStatusCode.ServiceUnavailable;
    var transient = false;
    try
    {
        await transport.DispatchAsync(dispatch);
    }
    catch (PaymentRequestEventTransportException ex)
    {
        transient = ex.FailureKind == PaymentRequestEventTransportFailureKind.Transient;
    }
    Assert(transient, "A destination 5xx must preserve transient outbox retry semantics.");

    Console.WriteLine("AFW-BE-REQUEST Commit 9 durable webhook subscription registry and multi-endpoint delivery scenarios: PASS");
}
finally
{
    Environment.SetEnvironmentVariable("AFW_TEST_WEBHOOK_ALPHA_SECRET", null);
    Environment.SetEnvironmentVariable("AFW_TEST_WEBHOOK_BETA_SECRET", null);
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}

sealed record Delivery(Uri Uri, string KeyId, string EventId);

sealed class RecordingHandler : HttpMessageHandler
{
    public List<Delivery> Deliveries { get; } = [];
    public Dictionary<string, HttpStatusCode> StatusByHost { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Reset() => Deliveries.Clear();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI missing.");
        var keyId = request.Headers.GetValues(PaymentRequestWebhookHeaderNames.KeyId).Single();
        var eventId = request.Headers.GetValues(PaymentRequestWebhookHeaderNames.EventId).Single();
        Deliveries.Add(new Delivery(uri, keyId, eventId));
        var status = StatusByHost.TryGetValue(uri.Host, out var configured)
            ? configured
            : HttpStatusCode.Accepted;
        return Task.FromResult(new HttpResponseMessage(status));
    }
}
