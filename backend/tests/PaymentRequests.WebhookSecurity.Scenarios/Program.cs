using AfriWallet.PaymentRequests.Webhooks;

var now = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
var current = new PaymentRequestWebhookSecret(
    "key-2026-09",
    "current-secret-material-32-bytes-minimum-value",
    now.AddDays(-1),
    now.AddDays(30));
var previous = new PaymentRequestWebhookSecret(
    "key-2026-08",
    "previous-secret-material-32-bytes-minimum-value",
    now.AddDays(-40),
    now.AddMinutes(3));

var secrets = new RotatingPaymentRequestWebhookSecretProvider(current, [previous]);
var clock = new MutableTimeProvider(now);
var options = new PaymentRequestWebhookSecurityOptions(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
var signer = new PaymentRequestWebhookSigner(secrets);
var verifier = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options);

var eventId = Guid.NewGuid();
var payload = """{"eventType":"payment-request.paid","paymentRequestId":"123"}"""u8.ToArray();
var headers = signer.Sign(eventId, payload, now);
Assert(headers.KeyId == current.KeyId, "Signer must use the current rotation key.");
Assert(headers.Signature.StartsWith("v1=", StringComparison.Ordinal), "Signature version prefix is required.");

var verified = verifier.Verify(new PaymentRequestWebhookVerificationRequest(payload, headers));
Assert(verified.Status == PaymentRequestWebhookVerificationStatus.Verified, "Valid signed webhook must verify.");

var replay = verifier.Verify(new PaymentRequestWebhookVerificationRequest(payload, headers));
Assert(replay.Status == PaymentRequestWebhookVerificationStatus.ReplayDetected, "Second use of the same event id must be rejected as replay.");

var tamperEvent = Guid.NewGuid();
var tamperHeaders = signer.Sign(tamperEvent, payload, now);
var tamperedPayload = """{"eventType":"payment-request.cancelled","paymentRequestId":"123"}"""u8.ToArray();
var tampered = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(tamperedPayload, tamperHeaders));
Assert(tampered.Status == PaymentRequestWebhookVerificationStatus.InvalidSignature, "Payload mutation must invalidate the signature.");

var whitespaceEvent = Guid.NewGuid();
var whitespaceHeaders = signer.Sign(whitespaceEvent, payload, now);
var whitespacePayload = ("""{"eventType":"payment-request.paid","paymentRequestId":"123"}""" + " ").Select(c => (byte)c).ToArray();
var whitespace = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(whitespacePayload, whitespaceHeaders));
Assert(whitespace.Status == PaymentRequestWebhookVerificationStatus.InvalidSignature, "Verifier must use exact payload bytes.");

var oldEvent = Guid.NewGuid();
var oldHeaders = signer.Sign(oldEvent, payload, now) with { TimestampUnixSeconds = now.AddMinutes(-6).ToUnixTimeSeconds() };
var stale = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(payload, oldHeaders));
Assert(stale.Status == PaymentRequestWebhookVerificationStatus.TimestampOutsideTolerance, "Stale timestamp must be rejected before replay registration.");

var futureEvent = Guid.NewGuid();
var futureHeaders = signer.Sign(futureEvent, payload, now) with { TimestampUnixSeconds = now.AddMinutes(6).ToUnixTimeSeconds() };
var future = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(payload, futureHeaders));
Assert(future.Status == PaymentRequestWebhookVerificationStatus.TimestampOutsideTolerance, "Excessive future timestamp must be rejected.");

var malformedEvent = Guid.NewGuid();
var malformedHeaders = signer.Sign(malformedEvent, payload, now) with { Signature = "v1=xyz" };
var malformed = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(payload, malformedHeaders));
Assert(malformed.Status == PaymentRequestWebhookVerificationStatus.InvalidSignature, "Malformed signature must be rejected.");

var unknownKeyEvent = Guid.NewGuid();
var unknownKeyHeaders = signer.Sign(unknownKeyEvent, payload, now) with { KeyId = "unknown-key" };
var unknownKey = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(payload, unknownKeyHeaders));
Assert(unknownKey.Status == PaymentRequestWebhookVerificationStatus.UnknownOrInactiveKey, "Unknown rotation key must be rejected.");

var previousSigner = new PaymentRequestWebhookSigner(
    new RotatingPaymentRequestWebhookSecretProvider(previous));
var previousEvent = Guid.NewGuid();
var previousHeaders = previousSigner.Sign(previousEvent, payload, now);
var previousVerified = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(payload, previousHeaders));
Assert(previousVerified.Status == PaymentRequestWebhookVerificationStatus.Verified, "Previous key must verify during the rotation overlap window.");

clock.Advance(TimeSpan.FromMinutes(4));
var expiredPrevious = new PaymentRequestWebhookVerifier(
    secrets,
    new InMemoryPaymentRequestWebhookReplayGuard(),
    clock,
    options).Verify(new PaymentRequestWebhookVerificationRequest(payload, previousHeaders with { EventId = Guid.NewGuid() }));
Assert(expiredPrevious.Status == PaymentRequestWebhookVerificationStatus.UnknownOrInactiveKey, "Previous key must stop verifying after its rotation window.");

var capacityGuard = new InMemoryPaymentRequestWebhookReplayGuard(1);
Assert(capacityGuard.TryAccept(Guid.NewGuid(), now, now.AddMinutes(10)), "First replay guard entry must be accepted.");
try
{
    capacityGuard.TryAccept(Guid.NewGuid(), now, now.AddMinutes(10));
    throw new InvalidOperationException("Expected replay guard capacity failure.");
}
catch (InvalidOperationException exception) when (exception.Message.Contains("capacity", StringComparison.OrdinalIgnoreCase))
{
}

Console.WriteLine("AFW-BE-REQUEST Commit 7 webhook security scenarios: PASS");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void Advance(TimeSpan delta) => utcNow = utcNow.Add(delta);
}
