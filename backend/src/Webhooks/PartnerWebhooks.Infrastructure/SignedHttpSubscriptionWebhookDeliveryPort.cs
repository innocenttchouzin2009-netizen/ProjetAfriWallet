using System.Net;
using System.Text;
using AfriWallet.PartnerWebhooks.Application;

namespace AfriWallet.PartnerWebhooks.Infrastructure;

public sealed class SignedHttpSubscriptionWebhookDeliveryPort(
    HttpClient httpClient,
    HmacSha256WebhookEventSigner signer)
    : ISubscriptionWebhookDeliveryPort
{
    public async Task DeliverAsync(
        SubscriptionWebhookDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        cancellationToken.ThrowIfCancellationRequested();

        var signed = await signer.SignAsync(
            delivery.Event,
            delivery.SigningSecretReference,
            cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, delivery.Endpoint.Value)
        {
            Content = new StringContent(
                signed.CanonicalBody,
                Encoding.UTF8,
                "application/json")
        };

        AddHeader(request, PartnerWebhookHttpHeaders.EventId, delivery.Event.EventId.Value.ToString("D"));
        AddHeader(request, PartnerWebhookHttpHeaders.EventType, delivery.Event.EventType.Value);
        AddHeader(
            request,
            PartnerWebhookHttpHeaders.Timestamp,
            delivery.Event.OccurredAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"));
        AddHeader(request, PartnerWebhookHttpHeaders.SignatureAlgorithm, signed.Algorithm);
        AddHeader(request, PartnerWebhookHttpHeaders.Signature, signed.Signature);
        AddHeader(request, PartnerWebhookHttpHeaders.SubscriptionId, delivery.SubscriptionId.Value.ToString("D"));
        AddHeader(request, PartnerWebhookHttpHeaders.PartnerId, delivery.PartnerId.Value);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Webhook endpoint returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                inner: null,
                response.StatusCode);
        }
    }

    private static void AddHeader(HttpRequestMessage request, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Webhook header '{name}' cannot be empty.");
        }

        if (!request.Headers.TryAddWithoutValidation(name, value))
        {
            throw new InvalidOperationException($"Webhook header '{name}' could not be added.");
        }
    }
}
