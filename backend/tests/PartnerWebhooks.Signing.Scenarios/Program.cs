using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;

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

var eventId = WebhookEventId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));
var eventType = WebhookEventType.From("payment.succeeded");
var occurredAt = new DateTimeOffset(2026, 9, 18, 12, 30, 0, TimeSpan.Zero);

using var firstDocument = JsonDocument.Parse("""
{
  "z": 9,
  "customer": {
    "name": "Ada",
    "id": "cust-1"
  },
  "items": [
    { "b": 2, "a": 1 },
    true
  ],
  "a": "first"
}
""");

using var secondDocument = JsonDocument.Parse("""
{
  "items": [
    { "a": 1, "b": 2 },
    true
  ],
  "a": "first",
  "z": 9,
  "customer": {
    "id": "cust-1",
    "name": "Ada"
  }
}
""");

var firstEvent = OutboundWebhookEvent.Create(eventId, eventType, occurredAt, firstDocument.RootElement);
var secondEvent = OutboundWebhookEvent.Create(eventId, eventType, occurredAt, secondDocument.RootElement);

var serializer = new CanonicalWebhookEventSerializer();
var firstBody = serializer.Serialize(firstEvent);
var secondBody = serializer.Serialize(secondEvent);

Assert(firstBody == secondBody, "Equivalent payloads with different object property order must serialize identically.");
Assert(firstBody ==
    """{"eventId":"11111111-2222-3333-4444-555555555555","type":"payment.succeeded","occurredAtUtc":"2026-09-18T12:30:00.0000000Z","payload":{"a":"first","customer":{"id":"cust-1","name":"Ada"},"items":[{"a":1,"b":2},true],"z":9}}""",
    "Canonical event body does not match the locked wire representation.");

var resolver = new RecordingSecretResolver("super-secret-key");
var signer = new HmacSha256WebhookEventSigner(serializer, resolver);
var secretReference = WebhookSigningSecretReference.From("vault://partners/acme/webhook-v1");
var signed = await signer.SignAsync(firstEvent, secretReference);

Assert(resolver.LastReference == secretReference, "Signer must forward the secret reference unchanged.");
Assert(signed.CanonicalBody == firstBody, "Signer must sign the exact canonical body.");
Assert(signed.Algorithm == "hmac-sha256", "Signing algorithm marker mismatch.");

using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("super-secret-key")))
{
    var expected = "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(firstBody))).ToLowerInvariant();
    Assert(signed.Signature == expected, "HMAC-SHA256 signature mismatch.");
}

var signedAgain = await signer.SignAsync(secondEvent, secretReference);
Assert(signedAgain.Signature == signed.Signature, "Equivalent canonical envelopes must produce the same signature.");

await AssertThrowsAsync<InvalidOperationException>(
    () => new HmacSha256WebhookEventSigner(serializer, new RecordingSecretResolver(null))
        .SignAsync(firstEvent, secretReference),
    "Missing signing secret must fail closed.");

using var undefinedDocument = JsonDocument.Parse("null");
var undefined = default(JsonElement);
try
{
    _ = OutboundWebhookEvent.Create(eventId, eventType, occurredAt, undefined);
    throw new InvalidOperationException("Undefined payload must be rejected.");
}
catch (ArgumentException)
{
}

try
{
    _ = OutboundWebhookEvent.Create(
        eventId,
        eventType,
        new DateTimeOffset(2026, 9, 18, 12, 30, 0, TimeSpan.FromHours(2)),
        undefinedDocument.RootElement);
    throw new InvalidOperationException("Non-UTC timestamp must be rejected.");
}
catch (ArgumentException)
{
}

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => signer.SignAsync(firstEvent, secretReference, cts.Token),
    "Cancellation must propagate before secret resolution.");

Console.WriteLine("AFW-BE-WEBHOOK-1 canonical event envelope and signing scenarios: PASS");

sealed class RecordingSecretResolver(string? secret) : IWebhookSigningSecretResolver
{
    public WebhookSigningSecretReference? LastReference { get; private set; }

    public Task<string?> ResolveAsync(
        WebhookSigningSecretReference secretReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastReference = secretReference;
        return Task.FromResult(secret);
    }
}
