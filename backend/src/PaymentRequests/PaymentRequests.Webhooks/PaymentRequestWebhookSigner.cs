using System.Security.Cryptography;

namespace AfriWallet.PaymentRequests.Webhooks;

public sealed class PaymentRequestWebhookSigner(
    IPaymentRequestWebhookSecretProvider secretProvider)
{
    public PaymentRequestWebhookHeaders Sign(
        Guid eventId,
        ReadOnlyMemory<byte> payload,
        DateTimeOffset signedAtUtc)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Webhook event id cannot be empty.", nameof(eventId));
        if (signedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Webhook timestamp must be UTC.", nameof(signedAtUtc));

        var secret = secretProvider.GetSigningSecret(signedAtUtc);
        var timestamp = signedAtUtc.ToUnixTimeSeconds();
        var digest = PaymentRequestWebhookCryptography.ComputeSignature(
            payload.Span,
            eventId,
            timestamp,
            secret.KeyId,
            secret.Secret);

        try
        {
            return new PaymentRequestWebhookHeaders(
                eventId,
                timestamp,
                secret.KeyId,
                PaymentRequestWebhookCryptography.SignaturePrefix +
                Convert.ToHexString(digest).ToLowerInvariant());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
        }
    }
}
