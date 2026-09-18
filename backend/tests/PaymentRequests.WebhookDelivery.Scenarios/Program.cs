using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
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

var occurredAt = new DateTimeOffset(2026, 9, 18, 10, 0, 0, TimeSpan.Zero);
var attemptedAt = occurredAt.AddSeconds(5);
var eventId = Guid.NewGuid();
var requestId = PaymentRequestId.From(Guid.NewGuid());
var envelope = new PaymentRequestEventEnvelope(
    eventId,
    requestId,
    "payment-request.created",
    occurredAt,
    "{\"paymentRequestId\":\"redacted-safe-id\"}");

var signer = new RecordingSigner(new PaymentRequestWebhookSignature("hmac-sha256", "sig-value"));
var transport = new RecordingTransport(PaymentRequestWebhookTransportResult.Delivered(202, "provider-request-1"));
var service = new PaymentRequestWebhookDeliveryService(signer, transport);

var result = await service.DeliverOnceAsync(
    envelope,
    new Uri("https://hooks.example.test/payment-requests"),
    attemptedAt);

Assert(result.EventId == eventId, "Attempt event id mismatch.");
Assert(result.AttemptedAtUtc == attemptedAt, "Attempt timestamp mismatch.");
Assert(result.Outcome == PaymentRequestWebhookDeliveryOutcome.Delivered, "Expected delivered outcome.");
Assert(result.StatusCode == 202, "Expected transport status code.");
Assert(result.ProviderRequestId == "provider-request-1", "Expected provider request id.");
Assert(signer.Calls == 1, "Signer must be invoked exactly once.");
Assert(transport.Calls == 1, "Transport must be invoked exactly once.");

var signedRequest = transport.LastRequest ?? throw new InvalidOperationException("Signed request was not captured.");
Assert(signedRequest.Endpoint == new Uri("https://hooks.example.test/payment-requests"), "Endpoint mismatch.");
Assert(signedRequest.HttpMethod == "POST", "Webhook method must be POST.");
Assert(signedRequest.ContentType == "application/json", "Webhook content type must be application/json.");
Assert(signedRequest.PayloadJson == envelope.PayloadJson, "Payload must be forwarded unchanged.");
Assert(signedRequest.Headers[PaymentRequestWebhookHeaders.EventId] == eventId.ToString(), "Event id header mismatch.");
Assert(signedRequest.Headers[PaymentRequestWebhookHeaders.EventType] == envelope.EventType, "Event type header mismatch.");
Assert(signedRequest.Headers[PaymentRequestWebhookHeaders.Timestamp] == attemptedAt.ToString("O"), "Timestamp header mismatch.");
Assert(signedRequest.Headers[PaymentRequestWebhookHeaders.SignatureAlgorithm] == "hmac-sha256", "Signature algorithm header mismatch.");
Assert(signedRequest.Headers[PaymentRequestWebhookHeaders.Signature] == "sig-value", "Signature header mismatch.");

var signingInput = signer.LastInput ?? throw new InvalidOperationException("Signing input was not captured.");
Assert(signingInput.EventId == eventId, "Signer event id mismatch.");
Assert(signingInput.EventType == envelope.EventType, "Signer event type mismatch.");
Assert(signingInput.OccurredAtUtc == occurredAt, "Signer occurred-at mismatch.");
Assert(signingInput.AttemptedAtUtc == attemptedAt, "Signer attempted-at mismatch.");
Assert(signingInput.PayloadJson == envelope.PayloadJson, "Signer payload mismatch.");

var transient = new PaymentRequestWebhookDeliveryService(
    new RecordingSigner(new PaymentRequestWebhookSignature("hmac-sha256", "sig")),
    new RecordingTransport(PaymentRequestWebhookTransportResult.TransientFailure(503, "UPSTREAM_UNAVAILABLE", "try later")));
var transientResult = await transient.DeliverOnceAsync(
    envelope,
    new Uri("https://hooks.example.test/payment-requests"),
    attemptedAt);
Assert(transientResult.Outcome == PaymentRequestWebhookDeliveryOutcome.TransientFailure, "Transient outcome must be preserved.");
Assert(transientResult.StatusCode == 503, "Transient status code must be preserved.");
Assert(transientResult.ErrorCode == "UPSTREAM_UNAVAILABLE", "Transient error code must be preserved.");

var permanent = new PaymentRequestWebhookDeliveryService(
    new RecordingSigner(new PaymentRequestWebhookSignature("hmac-sha256", "sig")),
    new RecordingTransport(PaymentRequestWebhookTransportResult.PermanentFailure(410, "ENDPOINT_GONE", "gone")));
var permanentResult = await permanent.DeliverOnceAsync(
    envelope,
    new Uri("https://hooks.example.test/payment-requests"),
    attemptedAt);
Assert(permanentResult.Outcome == PaymentRequestWebhookDeliveryOutcome.PermanentFailure, "Permanent outcome must be preserved.");

await AssertThrowsAsync<ArgumentException>(
    () => service.DeliverOnceAsync(envelope, new Uri("/relative", UriKind.Relative), attemptedAt),
    "Relative endpoint must be rejected.");

await AssertThrowsAsync<ArgumentException>(
    () => service.DeliverOnceAsync(envelope, new Uri("ftp://example.test/hook"), attemptedAt),
    "Non-HTTP endpoint must be rejected.");

await AssertThrowsAsync<ArgumentException>(
    () => service.DeliverOnceAsync(envelope, new Uri("https://example.test/hook"), occurredAt.AddSeconds(-1)),
    "Attempt before event time must be rejected.");

var invalidSignatureService = new PaymentRequestWebhookDeliveryService(
    new RecordingSigner(new PaymentRequestWebhookSignature("", "")),
    new RecordingTransport(PaymentRequestWebhookTransportResult.Delivered()));
await AssertThrowsAsync<InvalidOperationException>(
    () => invalidSignatureService.DeliverOnceAsync(envelope, new Uri("https://example.test/hook"), attemptedAt),
    "Blank signature contract must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.DeliverOnceAsync(envelope, new Uri("https://example.test/hook"), attemptedAt, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-REQUEST webhook delivery port and delivery attempt foundation scenarios: PASS");

sealed class RecordingSigner(PaymentRequestWebhookSignature signature) : IPaymentRequestWebhookSigner
{
    public int Calls { get; private set; }
    public PaymentRequestWebhookSignatureInput? LastInput { get; private set; }

    public Task<PaymentRequestWebhookSignature> SignAsync(
        PaymentRequestWebhookSignatureInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastInput = input;
        return Task.FromResult(signature);
    }
}

sealed class RecordingTransport(PaymentRequestWebhookTransportResult result) : IPaymentRequestWebhookTransport
{
    public int Calls { get; private set; }
    public PaymentRequestWebhookSignedRequest? LastRequest { get; private set; }

    public Task<PaymentRequestWebhookTransportResult> SendAsync(
        PaymentRequestWebhookSignedRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastRequest = request;
        return Task.FromResult(result);
    }
}
