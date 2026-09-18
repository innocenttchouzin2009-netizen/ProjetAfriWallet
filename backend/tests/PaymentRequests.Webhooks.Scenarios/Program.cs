using System.Net;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks.Infrastructure;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

const string payload = "{\"event\":\"payment-request.paid.v1\",\"amountMinor\":2500}";
var signer = new HmacSha256PaymentRequestWebhookSigner("secret-key");
var signature = signer.Sign(payload);
Assert(
    signature == "sha256=3bb9b965659415e609aafd21139fccca80dd415306e6fe74740d406ca75a63ba",
    "HMAC-SHA256 signature must match the stable test vector.");

var message = new PaymentRequestOutboxDeliveryMessage(
    Guid.Parse("11111111-2222-3333-4444-555555555555"),
    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
    "payment-request.paid.v1",
    payload,
    new DateTimeOffset(2026, 9, 18, 11, 30, 0, TimeSpan.Zero));
const string idempotencyKey = "11111111222233334444555555555555";
var options = new PaymentRequestWebhookTransportOptions(new Uri("https://webhooks.example.test/payment-requests"));

var successHandler = new RecordingHandler(HttpStatusCode.NoContent);
var successTransport = new HttpPaymentRequestOutboxTransport(
    new HttpClient(successHandler),
    signer,
    options);
await successTransport.DeliverAsync(message, idempotencyKey);

Assert(successHandler.SendCount == 1, "Transport must perform exactly one HTTP send per DeliverAsync call.");
Assert(successHandler.Method == HttpMethod.Post, "Webhook transport must use POST.");
Assert(successHandler.RequestUri == options.Endpoint, "Webhook endpoint mismatch.");
Assert(successHandler.Body == payload, "Webhook payload must be sent byte-for-byte as JSON text.");
Assert(successHandler.Headers["X-AfWal-Signature"].Single() == signature, "Signature header mismatch.");
Assert(successHandler.Headers["Idempotency-Key"].Single() == idempotencyKey, "Idempotency header mismatch.");
Assert(successHandler.Headers["X-AfWal-Event"].Single() == message.EventType, "Event header mismatch.");
Assert(successHandler.Headers["X-AfWal-Message-Id"].Single() == message.MessageId.ToString("N"), "Message id header mismatch.");

var failureHandler = new RecordingHandler(HttpStatusCode.ServiceUnavailable);
var failureTransport = new HttpPaymentRequestOutboxTransport(
    new HttpClient(failureHandler),
    signer,
    options);
try
{
    await failureTransport.DeliverAsync(message, idempotencyKey);
    throw new InvalidOperationException("Expected non-success HTTP response to fail delivery.");
}
catch (HttpRequestException)
{
}
Assert(failureHandler.SendCount == 1, "Concrete HTTP transport must not retry failed requests internally.");

using var cts = new CancellationTokenSource();
cts.Cancel();
var cancelledHandler = new RecordingHandler(HttpStatusCode.OK);
var cancelledTransport = new HttpPaymentRequestOutboxTransport(
    new HttpClient(cancelledHandler),
    signer,
    options);
try
{
    await cancelledTransport.DeliverAsync(message, idempotencyKey, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException)
{
}
Assert(cancelledHandler.SendCount == 0, "Cancelled delivery must not reach the HTTP handler.");

Assert(
    typeof(HttpPaymentRequestOutboxTransport)
        .GetConstructors()
        .Single()
        .GetParameters()
        .All(parameter => parameter.ParameterType != typeof(IPaymentRequestOutboxStore)),
    "Transport must not depend on outbox/delivery persistence.");

Console.WriteLine("AFW-BE-REQUEST-1 concrete HMAC signer and HTTP transport scenarios: PASS");

sealed class RecordingHandler(HttpStatusCode responseStatus) : HttpMessageHandler
{
    public int SendCount { get; private set; }
    public HttpMethod? Method { get; private set; }
    public Uri? RequestUri { get; private set; }
    public string? Body { get; private set; }
    public Dictionary<string, string[]> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SendCount++;
        Method = request.Method;
        RequestUri = request.RequestUri;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        foreach (var header in request.Headers)
        {
            Headers[header.Key] = header.Value.ToArray();
        }

        return new HttpResponseMessage(responseStatus);
    }
}
