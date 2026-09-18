using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AfriWallet.PaymentRequests.Application;

namespace AfriWallet.PaymentRequests.Webhooks;

public sealed class RegistryBackedHttpPaymentRequestEventTransport(
    HttpClient httpClient,
    IPaymentRequestWebhookSubscriptionRegistry registry,
    IPaymentRequestWebhookSigningSecretResolver secretResolver,
    IPaymentRequestWebhookDeliveryAttemptStore attemptStore,
    IPaymentRequestWebhookReliabilityProtector reliabilityProtector,
    TimeProvider timeProvider)
    : IPaymentRequestEventTransport
{
    public RegistryBackedHttpPaymentRequestEventTransport(
        HttpClient httpClient,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        IPaymentRequestWebhookSigningSecretResolver secretResolver,
        IPaymentRequestWebhookDeliveryAttemptStore attemptStore,
        TimeProvider timeProvider)
        : this(
            httpClient,
            registry,
            secretResolver,
            attemptStore,
            NoOpPaymentRequestWebhookReliabilityProtector.Instance,
            timeProvider)
    {
    }
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

        var startedAtUtc = timeProvider.GetUtcNow();
        var startedTimestamp = timeProvider.GetTimestamp();
        var outcome = PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure;
        int? httpStatusCode = null;

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
            httpStatusCode = (int)response.StatusCode;

            if (httpStatusCode is >= 200 and <= 299)
            {
                outcome = PaymentRequestWebhookDeliveryAttemptOutcome.Success;
                return;
            }

            if (httpStatusCode is >= 400 and <= 499)
            {
                outcome = PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure;
                throw new PaymentRequestEventTransportException(
                    PaymentRequestEventTransportFailureKind.Permanent,
                    $"Webhook receiver rejected event with HTTP {httpStatusCode}.");
            }

            outcome = PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure;
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                $"Webhook receiver failed with HTTP {httpStatusCode}.");
        }
        catch (TaskCanceledException exception)
        {
            outcome = PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure;
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "Webhook HTTP delivery timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            outcome = PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure;
            throw new PaymentRequestEventTransportException(
                PaymentRequestEventTransportFailureKind.Transient,
                "Webhook HTTP delivery failed before a receiver response was obtained.",
                exception);
        }
        finally
        {
            try
            {
                var completedAtUtc = timeProvider.GetUtcNow();
                var latency = timeProvider.GetElapsedTime(startedTimestamp);
                var latencyMilliseconds = Math.Max(0L, (long)Math.Ceiling(latency.TotalMilliseconds));

                await attemptStore.AppendAsync(
                    PaymentRequestWebhookDeliveryAttempt.Create(
                        destination.Id,
                        eventId,
                        outcome,
                        httpStatusCode,
                        latencyMilliseconds,
                        startedAtUtc,
                        completedAtUtc),
                    CancellationToken.None);

                // Reliability protection is deliberately post-attempt and must never
                // change the delivery outcome or create a second retry path beside Outbox.
                try
                {
                    await reliabilityProtector.EvaluateAndProtectAsync(
                        destination.Id,
                        CancellationToken.None);
                }
                catch (Exception)
                {
                    // A protection-evaluation failure is retried naturally on the next
                    // real delivery attempt. It must not make a successful webhook
                    // delivery look failed and cause a duplicate Outbox delivery.
                }
            }
            catch (PaymentRequestEventTransportException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new PaymentRequestEventTransportException(
                    PaymentRequestEventTransportFailureKind.Transient,
                    "Webhook delivery attempt could not be recorded.",
                    exception);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(digest);
            }
        }
    }
}
