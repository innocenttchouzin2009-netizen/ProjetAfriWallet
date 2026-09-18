using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Webhooks;

public sealed class RegistryBackedHttpPaymentRequestEventTransport(
    HttpClient httpClient,
    IPaymentRequestWebhookSubscriptionRegistry registry,
    IPaymentRequestWebhookSigningSecretResolver secretResolver,
    TimeProvider timeProvider)
    : IPaymentRequestEventTransport
{
    public async Task DispatchAsync(PaymentRequestEventDispatch dispatch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        cancellationToken.ThrowIfCancellationRequested();
        if (dispatch.EventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(dispatch));
        if (dispatch.PaymentRequestId == Guid.Empty) throw new ArgumentException("Payment request id cannot be empty.", nameof(dispatch));
        if (dispatch.OccurredAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Event timestamp must be UTC.", nameof(dispatch));
        if (string.IsNullOrWhiteSpace(dispatch.PayloadJson)) throw new ArgumentException("Event payload is required.", nameof(dispatch));

        var eventType = PaymentRequestWebhookSubscription.NormalizeEventType(dispatch.EventType);
        var destinations = await registry.ListActiveForEventAsync(eventType, cancellationToken);
        if (destinations.Count == 0) return;

        var body = Encoding.UTF8.GetBytes(dispatch.PayloadJson);
        var signedAtUtc = timeProvider.GetUtcNow();
        var transientFailures = 0;
        var permanentFailures = 0;

        foreach (var destination in destinations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await SendAsync(destination, dispatch.EventId, body, signedAtUtc, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (PaymentRequestEventTransportException exception)
            {
                if (exception.FailureKind == PaymentRequestEventTransportFailureKind.Transient) transientFailures++;
                else permanentFailures++;
            }
            catch (InvalidOperationException)
            {
                permanentFailures++;
            }
        }

        if (transientFailures > 0)
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                $"Webhook fan-out failed transiently for {transientFailures} destination(s).");

        if (permanentFailures > 0)
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Permanent,
                $"Webhook fan-out failed permanently for {permanentFailures} destination(s).");
    }

    private async Task SendAsync(
        PaymentRequestWebhookSubscription destination,
        Guid eventId,
        byte[] body,
        DateTimeOffset signedAtUtc,
        CancellationToken cancellationToken)
    {
        var secret = await secretResolver.ResolveAsync(destination.SecretReference, cancellationToken);
        var timestamp = signedAtUtc.ToUnixTimeSeconds();
        var digest = PaymentRequestWebhookCryptography.ComputeSignature(
            body, eventId, timestamp, destination.KeyId, secret);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, destination.Endpoint);
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.EventId, eventId.ToString("D"));
            request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.Timestamp, timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
            request.Headers.TryAddWithoutValidation(PaymentRequestWebhookHeaderNames.KeyId, destination.KeyId);
            request.Headers.TryAddWithoutValidation(
                PaymentRequestWebhookHeaderNames.Signature,
                PaymentRequestWebhookCryptography.SignaturePrefix + Convert.ToHexString(digest).ToLowerInvariant());
            request.Headers.TryAddWithoutValidation("X-AfWal-Webhook-Subscription-Id", destination.Id.ToString("D"));
            request.Headers.TryAddWithoutValidation("X-AfWal-Integration-Id", destination.IntegrationId);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var statusCode = (int)response.StatusCode;
            if (statusCode is >= 200 and <= 299) return;
            if (statusCode is >= 400 and <= 499)
                throw new PaymentRequestEventTransportException(
                    PaymentRequestEventTransportFailureKind.Permanent,
                    $"Webhook receiver rejected event with HTTP {statusCode}.");
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                $"Webhook receiver failed with HTTP {statusCode}.");
        }
        catch (TaskCanceledException exception)
        {
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "Webhook HTTP delivery timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "Webhook HTTP delivery failed before a receiver response was obtained.",
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
        }
    }
}
