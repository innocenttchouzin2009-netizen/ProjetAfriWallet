using System.Security.Cryptography;
using System.Text;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Webhooks.Infrastructure;

public sealed class HmacSha256PaymentRequestWebhookSigner : IPaymentRequestWebhookSigner
{
    private readonly byte[] secretBytes;

    public HmacSha256PaymentRequestWebhookSigner(string signingSecret)
    {
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            throw new ArgumentException("Webhook signing secret is required.", nameof(signingSecret));
        }

        secretBytes = Encoding.UTF8.GetBytes(signingSecret);
    }

    public string Sign(string payloadJson)
    {
        ArgumentNullException.ThrowIfNull(payloadJson);

        using var hmac = new HMACSHA256(secretBytes);
        var digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
        return "sha256=" + Convert.ToHexString(digest).ToLowerInvariant();
    }
}
