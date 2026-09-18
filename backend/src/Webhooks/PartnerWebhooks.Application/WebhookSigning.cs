using System.Security.Cryptography;
using System.Text;
using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public interface IWebhookSigningSecretResolver
{
    Task<string?> ResolveAsync(
        WebhookSigningSecretReference secretReference,
        CancellationToken cancellationToken = default);
}

public sealed record SignedWebhookEvent(
    string CanonicalBody,
    string Signature,
    string Algorithm);

public sealed class HmacSha256WebhookEventSigner(
    IWebhookEventSerializer serializer,
    IWebhookSigningSecretResolver secretResolver)
{
    public async Task<SignedWebhookEvent> SignAsync(
        OutboundWebhookEvent webhookEvent,
        WebhookSigningSecretReference secretReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretReference.Value))
        {
            throw new ArgumentException("Webhook signing secret reference is required.", nameof(secretReference));
        }

        var secret = await secretResolver.ResolveAsync(secretReference, cancellationToken);
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("Webhook signing secret could not be resolved.");
        }

        var canonicalBody = serializer.Serialize(webhookEvent);
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(canonicalBody);

        try
        {
            using var hmac = new HMACSHA256(keyBytes);
            var digest = hmac.ComputeHash(bodyBytes);
            var signature = "sha256=" + Convert.ToHexString(digest).ToLowerInvariant();
            return new SignedWebhookEvent(canonicalBody, signature, "hmac-sha256");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }
}
