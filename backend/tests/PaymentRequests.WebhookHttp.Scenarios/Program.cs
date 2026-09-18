using System.Net;
using System.Net.Http.Json;
using System.Text;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task<T> AssertThrowsAsync<T>(Func<Task> action, string message)
    where T : Exception
{
    try
    {
        await action();
    }
    catch (T exception)
    {
        return exception;
    }

    throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 18, 14, 0, 0, TimeSpan.Zero);
var timeProvider = new FixedTimeProvider(now);
var secretProvider = new RotatingPaymentRequestWebhookSecretProvider(
    new PaymentRequestWebhookSecret(
        "key-2026-09",
        "0123456789abcdef0123456789abcdef",
        DateTimeOffset.UnixEpoch));

var signer = new PaymentRequestWebhookSigner(secretProvider);
var endpoint = new Uri("https://receiver.example.test/hooks/payment-requests");
var dispatch = new PaymentRequestEventDispatch(
    Guid.Parse("11111111-2222-3333-4444-555555555555"),
    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
    "payment-request.paid",
    now.AddMinutes(-1),
    """{"paymentRequestId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","status":"Paid"}""");

var successHandler = new RecordingHandler(HttpStatusCode.Accepted);
var successTransport = new HttpPaymentRequestEventTransport(
    new HttpClient(successHandler),
    signer,
    new PaymentRequestWebhookHttpDeliveryOptions(endpoint),
    timeProvider);

await successTransport.DispatchAsync(dispatch);
Assert(successHandler.LastRequest is not null, "HTTP request must be sent.");
Assert(successHandler.LastBody == dispatch.PayloadJson, "Webhook body bytes must preserve the stored JSON payload.");
Assert(successHandler.LastRequest!.Method == HttpMethod.Post, "Webhook delivery must use POST.");
Assert(successHandler.LastRequest.RequestUri == endpoint, "Webhook endpoint mismatch.");
Assert(successHandler.Header(PaymentRequestWebhookHeaderNames.EventId) == dispatch.EventId.ToString("D"), "Event id header mismatch.");
Assert(successHandler.Header(PaymentRequestWebhookHeaderNames.KeyId) == "key-2026-09", "Key id header mismatch.");
Assert(successHandler.Header(PaymentRequestWebhookHeaderNames.Timestamp) == now.ToUnixTimeSeconds().ToString(), "Timestamp header mismatch.");
Assert(successHandler.Header(PaymentRequestWebhookHeaderNames.Signature).StartsWith("v1=", StringComparison.Ordinal), "Signature header must use v1 HMAC format.");

var verifier = new PaymentRequestWebhookVerifier(
    secretProvider,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    timeProvider);
var verified = verifier.Verify(new PaymentRequestWebhookVerificationRequest(
    Encoding.UTF8.GetBytes(successHandler.LastBody!),
    new PaymentRequestWebhookHeaders(
        dispatch.EventId,
        long.Parse(successHandler.Header(PaymentRequestWebhookHeaderNames.Timestamp)),
        successHandler.Header(PaymentRequestWebhookHeaderNames.KeyId),
        successHandler.Header(PaymentRequestWebhookHeaderNames.Signature))));
Assert(verified.Succeeded, "Transport headers and exact body must verify with the Commit 7 contract.");

var permanentTransport = new HttpPaymentRequestEventTransport(
    new HttpClient(new RecordingHandler(HttpStatusCode.BadRequest)),
    signer,
    new PaymentRequestWebhookHttpDeliveryOptions(endpoint),
    timeProvider);
var permanent = await AssertThrowsAsync<PaymentRequestEventTransportException>(
    () => permanentTransport.DispatchAsync(dispatch),
    "HTTP 4xx must become a permanent outbox delivery failure.");
Assert(permanent.FailureKind == PaymentRequestEventTransportFailureKind.Permanent, "HTTP 4xx classification mismatch.");

var transientTransport = new HttpPaymentRequestEventTransport(
    new HttpClient(new RecordingHandler(HttpStatusCode.ServiceUnavailable)),
    signer,
    new PaymentRequestWebhookHttpDeliveryOptions(endpoint),
    timeProvider);
var transient = await AssertThrowsAsync<PaymentRequestEventTransportException>(
    () => transientTransport.DispatchAsync(dispatch),
    "HTTP 5xx must become a transient outbox delivery failure.");
Assert(transient.FailureKind == PaymentRequestEventTransportFailureKind.Transient, "HTTP 5xx classification mismatch.");

var networkTransport = new HttpPaymentRequestEventTransport(
    new HttpClient(new ThrowingHandler()),
    signer,
    new PaymentRequestWebhookHttpDeliveryOptions(endpoint),
    timeProvider);
var network = await AssertThrowsAsync<PaymentRequestEventTransportException>(
    () => networkTransport.DispatchAsync(dispatch),
    "Network failure must become a transient outbox delivery failure.");
Assert(network.FailureKind == PaymentRequestEventTransportFailureKind.Transient, "Network classification mismatch.");

var receiverReplayGuard = new InMemoryPaymentRequestWebhookReplayGuard();
var receiverVerifier = new PaymentRequestWebhookVerifier(
    secretProvider,
    receiverReplayGuard,
    timeProvider);

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseTestServer();
builder.Services.AddSingleton(receiverVerifier);
var app = builder.Build();
app.MapReferencePaymentRequestWebhookReceiver();
await app.StartAsync();

var client = app.GetTestClient();
var valid = NewReceiverRequest(
    dispatch.EventId,
    dispatch.PayloadJson,
    signer.Sign(dispatch.EventId, Encoding.UTF8.GetBytes(dispatch.PayloadJson), now));
var validResponse = await client.SendAsync(valid);
Assert(validResponse.StatusCode == HttpStatusCode.Accepted, "Verified receiver request must return 202.");
var validResult = await validResponse.Content.ReadFromJsonAsync<ReceiverResult>();
Assert(validResult?.Code == "WEBHOOK_VERIFIED", "Receiver must report WEBHOOK_VERIFIED.");

var replay = NewReceiverRequest(
    dispatch.EventId,
    dispatch.PayloadJson,
    signer.Sign(dispatch.EventId, Encoding.UTF8.GetBytes(dispatch.PayloadJson), now));
var replayResponse = await client.SendAsync(replay);
Assert(replayResponse.StatusCode == HttpStatusCode.Conflict, "Replay must be rejected with 409.");
var replayResult = await replayResponse.Content.ReadFromJsonAsync<ReceiverResult>();
Assert(replayResult?.Code == "WEBHOOK_REPLAY_DETECTED", "Receiver replay code mismatch.");

var tamperedId = Guid.NewGuid();
var tamperedHeaders = signer.Sign(tamperedId, Encoding.UTF8.GetBytes(dispatch.PayloadJson), now);
var tampered = NewReceiverRequest(tamperedId, dispatch.PayloadJson + " ", tamperedHeaders);
var tamperedResponse = await client.SendAsync(tampered);
Assert(tamperedResponse.StatusCode == HttpStatusCode.Unauthorized, "Tampered body must be rejected before business logic.");
var tamperedResult = await tamperedResponse.Content.ReadFromJsonAsync<ReceiverResult>();
Assert(tamperedResult?.Code == "WEBHOOK_INVALID_SIGNATURE", "Tampered body signature code mismatch.");

timeProvider.SetUtcNow(now.AddMinutes(6));
var staleId = Guid.NewGuid();
var staleHeaders = signer.Sign(staleId, Encoding.UTF8.GetBytes(dispatch.PayloadJson), now);
var stale = NewReceiverRequest(staleId, dispatch.PayloadJson, staleHeaders);
var staleResponse = await client.SendAsync(stale);
Assert(staleResponse.StatusCode == HttpStatusCode.Unauthorized, "Stale timestamp must be rejected.");
var staleResult = await staleResponse.Content.ReadFromJsonAsync<ReceiverResult>();
Assert(staleResult?.Code == "WEBHOOK_TIMESTAMP_OUTSIDE_TOLERANCE", "Stale timestamp code mismatch.");

await app.DisposeAsync();
Console.WriteLine("AFW-BE-REQUEST Commit 8 signed webhook HTTP delivery and receiver scenarios: PASS");

static HttpRequestMessage NewReceiverRequest(
    Guid eventId,
    string body,
    PaymentRequestWebhookHeaders headers)
{
    var request = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/v1/payment-request-webhooks/reference");
    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
    request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.EventId, eventId.ToString("D"));
    request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.Timestamp, headers.TimestampUnixSeconds.ToString());
    request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.KeyId, headers.KeyId);
    request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.Signature, headers.Signature);
    return request;
}

sealed record ReceiverResult(string Code);

sealed class FixedTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private DateTimeOffset current = initial;
    public override DateTimeOffset GetUtcNow() => current;
    public void SetUtcNow(DateTimeOffset value) => current = value;
}

sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastBody { get; private set; }
    private readonly Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

    public string Header(string name) => headers.TryGetValue(name, out var value)
        ? value
        : throw new InvalidOperationException($"Missing header {name}.");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        foreach (var pair in request.Headers)
            headers[pair.Key] = pair.Value.Single();
        return new HttpResponseMessage(statusCode);
    }
}

sealed class ThrowingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        throw new HttpRequestException("network unavailable");
}
