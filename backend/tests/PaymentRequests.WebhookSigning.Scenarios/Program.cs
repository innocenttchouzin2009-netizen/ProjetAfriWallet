using System.Text;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Infrastructure;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var signer = new HmacSha256PaymentRequestWebhookSigner();
var secret = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
var signedAt = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
var dispatch = new PaymentRequestEventDispatch(
    Guid.Parse("11111111-1111-1111-1111-111111111111"),
    Guid.Parse("22222222-2222-2222-2222-222222222222"),
    "payment-request.paid",
    new DateTimeOffset(2026, 9, 18, 11, 59, 30, TimeSpan.Zero),
    "{\"version\":1,\"status\":\"Paid\",\"amountMinor\":2500}");

var request = new PaymentRequestWebhookSigningRequest(dispatch, signedAt, secret);
var signature = signer.Sign(request);

Assert(signature.Version == "v1", "Signature version must be v1.");
Assert(signature.TimestampUnixSeconds == 1789732800, "Signature timestamp mismatch.");
Assert(
    signature.DigestHex == "3d5b7c430b99afec5ede3b2c9fb5663392a47beb8e3ac10be12db03a277b3bc2",
    "Deterministic HMAC-SHA256 digest mismatch.");
Assert(
    signature.HeaderValue == "v1=3d5b7c430b99afec5ede3b2c9fb5663392a47beb8e3ac10be12db03a277b3bc2",
    "Signature header value mismatch.");
Assert(signer.Verify(request, signature), "Generated signature must verify.");

var tamperedPayload = request with
{
    Dispatch = dispatch with { PayloadJson = dispatch.PayloadJson.Replace("2500", "2501", StringComparison.Ordinal) }
};
Assert(!signer.Verify(tamperedPayload, signature), "Tampered payload must fail verification.");

var tamperedEventType = request with
{
    Dispatch = dispatch with { EventType = "payment-request.cancelled" }
};
Assert(!signer.Verify(tamperedEventType, signature), "Tampered event type must fail verification.");

var tamperedTimestamp = request with { SignedAtUtc = signedAt.AddSeconds(1) };
Assert(!signer.Verify(tamperedTimestamp, signature), "Tampered signature timestamp must fail verification.");

var malformedSignature = signature with { DigestHex = new string('z', 64) };
Assert(!signer.Verify(request, malformedSignature), "Malformed hex digest must fail verification.");

var wrongVersion = signature with { Version = "v2" };
Assert(!signer.Verify(request, wrongVersion), "Unknown signature version must fail verification.");

AssertThrows<ArgumentException>(
    () => signer.Sign(request with { Secret = Encoding.UTF8.GetBytes("too-short") }),
    "Short signing secrets must be rejected.");

AssertThrows<ArgumentException>(
    () => signer.Sign(request with { SignedAtUtc = signedAt.ToOffset(TimeSpan.FromHours(2)) }),
    "Non-UTC signature timestamp must be rejected.");

AssertThrows<ArgumentException>(
    () => signer.Sign(request with { Dispatch = dispatch with { EventId = Guid.Empty } }),
    "Empty event id must be rejected.");

Console.WriteLine("AFW-BE-REQUEST-WEBHOOK-1 HMAC-SHA256 signing scenarios: PASS");
