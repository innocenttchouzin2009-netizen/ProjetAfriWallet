using System.Net.Http.Headers;
using System.Text;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Webhooks;

public sealed record PaymentRequestWebhookHttpDeliveryOptions(Uri Endpoint)
{
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Endpoint);
        if (!Endpoint.IsAbsoluteUri ||
            (Endpoint.Scheme != Uri.UriSchemeHttps && Endpoint.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "Payment request webhook endpoint must be an absolute HTTP or HTTPS URI.");
        }
    }
}

public sealed class HttpPaymentRequestEventTransport(
    HttpClient httpClient,
    PaymentRequestWebhookSigner signer,
    PaymentRequestWebhookHttpDeliveryOptions options,
    TimeProvider timeProvider)
    : IPaymentRequestEventTransport
{
    public async Task DispatchAsync(
        PaymentRequestEventDispatch dispatch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (dispatch.EventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(dispatch));
        if (dispatch.PaymentRequestId == Guid.Empty)
            throw new ArgumentException("Payment request id cannot be empty.", nameof(dispatch));
        if (string.IsNullOrWhiteSpace(dispatch.EventType))
            throw new ArgumentException("Event type is required.", nameof(dispatch));
        if (dispatch.OccurredAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Event timestamp must be UTC.", nameof(dispatch));
        if (string.IsNullOrWhiteSpace(dispatch.PayloadJson))
            throw new ArgumentException("Event payload is required.", nameof(dispatch));

        var body = Encoding.UTF8.GetBytes(dispatch.PayloadJson);
        var signedAtUtc = timeProvider.GetUtcNow();
        var headers = signer.Sign(dispatch.EventId, body, signedAtUtc);

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };
        request.Headers.TryAddWithoutValidation(
            PaymentRequestWebhookHeaderNames.EventId,
            headers.EventId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PaymentRequestWebhookHeaderNames.Timestamp,
            headers.TimestampUnixSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation(
            PaymentRequestWebhookHeaderNames.KeyId,
            headers.KeyId);
        request.Headers.TryAddWithoutValidation(
            PaymentRequestWebhookHeaderNames.Signature,
            headers.Signature);

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var statusCode = (int)response.StatusCode;
            if (statusCode is >= 200 and <= 299)
                return;

            if (statusCode is >= 400 and <= 499)
            {
                throw new PaymentRequestEventTransportException(
                    PaymentRequestEventTransportFailureKind.Permanent,
                    $"Webhook receiver rejected event with HTTP {statusCode}.");
            }

            if (statusCode is >= 500 and <= 599)
            {
                throw new PaymentRequestEventTransportException(
                    PaymentRequestEventTransportFailureKind.Transient,
                    $"Webhook receiver failed with HTTP {statusCode}.");
            }

            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                $"Webhook receiver returned unexpected HTTP {statusCode}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PaymentRequestEventTransportException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "Webhook HTTP delivery failed before a receiver response was obtained.",
                exception);
        }
        catch (TaskCanceledException exception)
        {
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "Webhook HTTP delivery timed out.",
                exception);
        }
    }
}
