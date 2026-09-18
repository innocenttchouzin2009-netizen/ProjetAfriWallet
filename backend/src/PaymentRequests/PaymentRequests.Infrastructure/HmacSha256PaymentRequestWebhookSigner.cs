using System.Security.Cryptography;
using System.Text;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Infrastructure;

public sealed class HmacSha256PaymentRequestWebhookSigner : IPaymentRequestWebhookSigner
{
    public const string SignatureVersion = "v1";
    private const int MinimumSecretLengthBytes = 32;

    public PaymentRequestWebhookSignature Sign(PaymentRequestWebhookSigningRequest request)
    {
        ValidateRequest(request);

        var timestamp = request.SignedAtUtc.ToUnixTimeSeconds();
        var canonical = BuildCanonicalPayload(request.Dispatch, timestamp);
        var canonicalBytes = Encoding.UTF8.GetBytes(canonical);

        using var hmac = new HMACSHA256(request.Secret.ToArray());
        var digest = hmac.ComputeHash(canonicalBytes);

        return new PaymentRequestWebhookSignature(
            SignatureVersion,
            timestamp,
            Convert.ToHexString(digest).ToLowerInvariant());
    }

    public bool Verify(
        PaymentRequestWebhookSigningRequest request,
        PaymentRequestWebhookSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        ValidateRequest(request);

        if (!string.Equals(signature.Version, SignatureVersion, StringComparison.Ordinal) ||
            signature.TimestampUnixSeconds != request.SignedAtUtc.ToUnixTimeSeconds() ||
            signature.DigestHex.Length != 64)
        {
            return false;
        }

        byte[] suppliedDigest;
        try
        {
            suppliedDigest = Convert.FromHexString(signature.DigestHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = Sign(request);
        var expectedDigest = Convert.FromHexString(expected.DigestHex);

        return suppliedDigest.Length == expectedDigest.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedDigest, expectedDigest);
    }

    internal static string BuildCanonicalPayload(
        PaymentRequestEventDispatch dispatch,
        long timestampUnixSeconds)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        return string.Join(
            "\n",
            SignatureVersion,
            timestampUnixSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            dispatch.EventId.ToString("D"),
            dispatch.PaymentRequestId.ToString("D"),
            dispatch.EventType,
            dispatch.OccurredAtUtc.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            dispatch.PayloadJson);
    }

    private static void ValidateRequest(PaymentRequestWebhookSigningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Dispatch);

        if (request.Dispatch.EventId == Guid.Empty)
        {
            throw new ArgumentException("Webhook event id cannot be empty.", nameof(request));
        }

        if (request.Dispatch.PaymentRequestId == Guid.Empty)
        {
            throw new ArgumentException("Webhook payment request id cannot be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Dispatch.EventType))
        {
            throw new ArgumentException("Webhook event type is required.", nameof(request));
        }

        if (request.Dispatch.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Webhook event timestamp must be UTC.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Dispatch.PayloadJson))
        {
            throw new ArgumentException("Webhook payload is required.", nameof(request));
        }

        if (request.SignedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Webhook signature timestamp must be UTC.", nameof(request));
        }

        if (request.Secret.Length < MinimumSecretLengthBytes)
        {
            throw new ArgumentException(
                $"Webhook signing secret must contain at least {MinimumSecretLengthBytes} bytes.",
                nameof(request));
        }
    }
}
