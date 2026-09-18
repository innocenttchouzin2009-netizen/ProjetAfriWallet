using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;
using AfriWallet.PartnerWebhooks.Infrastructure;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var occurredAt = new DateTimeOffset(2026, 9, 18, 16, 30, 0, TimeSpan.Zero);
using var payloadDocument = JsonDocument.Parse("""{"amountMinor":2500,"paymentRequestId":"req-42"}""");

var webhookEvent = OutboundWebhookEvent.Create(
    WebhookEventId.From(Guid.Parse("11111111-2222-3333-4444-555555555555")),
    WebhookEventType.From("payment.succeeded"),
    occurredAt,
    payloadDocument.RootElement);

var alphaSecretRef = WebhookSigningSecretReference.From("vault://partners/alpha/webhook-v1");
var betaSecretRef = WebhookSigningSecretReference.From("vault://partners/beta/webhook-v3");

var resolver = new RecordingSecretResolver(new Dictionary<string, string>(StringComparer.Ordinal)
{
    [alphaSecretRef.Value] = "alpha-secret",
    [betaSecretRef.Value] = "beta-secret"
});

var serializer = new CanonicalWebhookEventSerializer();
var signer = new HmacSha256WebhookEventSigner(serializer, resolver);
var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
var client = new HttpClient(handler);
var port = new SignedHttpSubscriptionWebhookDeliveryPort(client, signer);

var alpha = new SubscriptionWebhookDelivery(
    PartnerWebhookSubscriptionId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
    PartnerId.From("partner.alpha"),
    WebhookEndpoint.From("https://alpha.example/webhooks"),
    alphaSecretRef,
    webhookEvent);

var beta = new SubscriptionWebhookDelivery(
    PartnerWebhookSubscriptionId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
    PartnerId.From("partner.beta"),
    WebhookEndpoint.From("https://beta.example/hooks/payment"),
    betaSecretRef,
    webhookEvent);

await port.DeliverAsync(alpha);
await port.DeliverAsync(beta);

Assert(handler.Requests.Count == 2, "Exactly one HTTP request must be sent per delivery.");
Assert(resolver.References.SequenceEqual([alphaSecretRef, betaSecretRef]), "Each delivery must resolve its own subscription secret reference.");

var canonicalBody = serializer.Serialize(webhookEvent);
Assert(handler.Requests.All(request => request.Method == HttpMethod.Post), "Webhook delivery must use POST.");
Assert(handler.Requests[0].Uri == new Uri(alpha.Endpoint.Value), "Alpha endpoint mismatch.");
Assert(handler.Requests[1].Uri == new Uri(beta.Endpoint.Value), "Beta endpoint mismatch.");
Assert(handler.Requests.All(request => request.ContentType == "application/json; charset=utf-8"), "Webhook content type mismatch.");
Assert(handler.Requests.All(request => request.Body == canonicalBody), "HTTP body must be the existing canonical serialization.");

AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.EventId, webhookEvent.EventId.Value.ToString("D"));
AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.EventType, webhookEvent.EventType.Value);
AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.Timestamp, "2026-09-18T16:30:00.0000000Z");
AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.SignatureAlgorithm, "hmac-sha256");
AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.SubscriptionId, alpha.SubscriptionId.Value.ToString("D"));
AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.PartnerId, alpha.PartnerId.Value);
AssertHeader(handler.Requests[1], PartnerWebhookHttpHeaders.SubscriptionId, beta.SubscriptionId.Value.ToString("D"));
AssertHeader(handler.Requests[1], PartnerWebhookHttpHeaders.PartnerId, beta.PartnerId.Value);

var alphaExpectedSignature = ComputeSignature("alpha-secret", canonicalBody);
var betaExpectedSignature = ComputeSignature("beta-secret", canonicalBody);
AssertHeader(handler.Requests[0], PartnerWebhookHttpHeaders.Signature, alphaExpectedSignature);
AssertHeader(handler.Requests[1], PartnerWebhookHttpHeaders.Signature, betaExpectedSignature);
Assert(alphaExpectedSignature != betaExpectedSignature, "Different subscription secrets must produce different signatures.");

Assert(!handler.Requests[0].Headers.Values.Any(value => value.Contains(alphaSecretRef.Value, StringComparison.Ordinal)), "Secret reference must never be emitted in webhook headers.");
Assert(!handler.Requests[0].Body.Contains(alphaSecretRef.Value, StringComparison.Ordinal), "Secret reference must never be emitted in webhook body.");
Assert(!handler.Requests[0].Headers.Values.Any(value => value.Contains("alpha-secret", StringComparison.Ordinal)), "Resolved secret must never be emitted.");

var failingHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
var failingPort = new SignedHttpSubscriptionWebhookDeliveryPort(new HttpClient(failingHandler), signer);
await AssertThrowsAsync<HttpRequestException>(
    () => failingPort.DeliverAsync(alpha),
    "Non-success HTTP response must fail the delivery.");

var missingSecretSigner = new HmacSha256WebhookEventSigner(
    serializer,
    new RecordingSecretResolver(new Dictionary<string, string>(StringComparer.Ordinal)));
var missingSecretPort = new SignedHttpSubscriptionWebhookDeliveryPort(new HttpClient(handler), missingSecretSigner);
await AssertThrowsAsync<InvalidOperationException>(
    () => missingSecretPort.DeliverAsync(alpha),
    "Missing signing secret must fail closed before HTTP delivery.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => port.DeliverAsync(alpha, cts.Token),
    "Cancellation must propagate before signing and HTTP transport.");

Console.WriteLine("AFW-BE-WEBHOOK-ROUTING-1 signed HTTP delivery per subscription scenarios: PASS");

static void AssertHeader(RecordedRequest request, string name, string expected)
{
    Assert(request.Headers.TryGetValue(name, out var actual), $"Missing header {name}.");
    Assert(actual == expected, $"Header {name} mismatch.");
}

static string ComputeSignature(string secret, string body)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
}

sealed class RecordingSecretResolver(IReadOnlyDictionary<string, string> secrets) : IWebhookSigningSecretResolver
{
    public List<WebhookSigningSecretReference> References { get; } = [];

    public Task<string?> ResolveAsync(
        WebhookSigningSecretReference secretReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        References.Add(secretReference);
        return Task.FromResult(secrets.TryGetValue(secretReference.Value, out var secret) ? secret : null);
    }
}

sealed record RecordedRequest(
    HttpMethod Method,
    Uri? Uri,
    string Body,
    string? ContentType,
    IReadOnlyDictionary<string, string> Headers);

sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => string.Join(",", header.Value),
            StringComparer.OrdinalIgnoreCase);

        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri,
            body,
            request.Content?.Headers.ContentType?.ToString(),
            headers));

        return responseFactory(request);
    }
}
