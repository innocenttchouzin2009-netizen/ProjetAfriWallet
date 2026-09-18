using System.Net.Http.Headers;
using System.Text;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Webhooks.Infrastructure;

public sealed class HttpPaymentRequestOutboxTransport(
    HttpClient httpClient,
    IPaymentRequestWebhookSigner signer,
    PaymentRequestWebhookTransportOptions options)
    : IPaymentRequestOutboxTransport
{
    public async Task DeliverAsync(
        PaymentRequestOutboxDeliveryMessage message,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        request.Content = new StringContent(message.PayloadJson, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation(options.SignatureHeaderName, signer.Sign(message.PayloadJson));
        request.Headers.TryAddWithoutValidation(options.IdempotencyHeaderName, idempotencyKey);
        request.Headers.TryAddWithoutValidation(options.EventTypeHeaderName, message.EventType);
        request.Headers.TryAddWithoutValidation(options.MessageIdHeaderName, message.MessageId.ToString("N"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
