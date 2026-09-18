using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AfriWallet.PaymentRequests.Webhooks;

public static class PaymentRequestWebhookReceiverEndpoints
{
    private const int MaximumBodyBytes = 256 * 1024;

    public static IEndpointRouteBuilder MapReferencePaymentRequestWebhookReceiver(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/v1/payment-request-webhooks/reference",
            VerifyReferenceReceiverAsync);

        return endpoints;
    }

    private static async Task<IResult> VerifyReferenceReceiverAsync(
        HttpRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var verifier = serviceProvider.GetService<PaymentRequestWebhookVerifier>();
        if (verifier is null)
        {
            return Results.Json(
                new { code = "WEBHOOK_RECEIVER_NOT_CONFIGURED" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!TryReadHeaders(request, out var headers))
        {
            return Results.BadRequest(new { code = "WEBHOOK_INVALID_REQUEST" });
        }

        byte[] payload;
        try
        {
            payload = await ReadBodyAsync(request, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { code = "WEBHOOK_INVALID_REQUEST" },
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        var verification = verifier.Verify(
            new PaymentRequestWebhookVerificationRequest(payload, headers!));

        if (!verification.Succeeded)
        {
            return verification.Status switch
            {
                PaymentRequestWebhookVerificationStatus.InvalidRequest =>
                    Results.BadRequest(new { code = verification.Code }),
                PaymentRequestWebhookVerificationStatus.ReplayDetected =>
                    Results.Conflict(new { code = verification.Code }),
                _ =>
                    Results.Json(
                        new { code = verification.Code },
                        statusCode: StatusCodes.Status401Unauthorized)
            };
        }

        // Reference endpoint intentionally performs no business processing before verification.
        return Results.Accepted(
            value: new
            {
                code = verification.Code,
                eventId = headers!.EventId
            });
    }

    private static bool TryReadHeaders(
        HttpRequest request,
        out PaymentRequestWebhookHeaders? headers)
    {
        headers = null;

        if (!request.Headers.TryGetValue(PaymentRequestWebhookHeaderNames.EventId, out var eventIdRaw) ||
            !Guid.TryParse(eventIdRaw.ToString(), out var eventId) ||
            eventId == Guid.Empty ||
            !request.Headers.TryGetValue(PaymentRequestWebhookHeaderNames.Timestamp, out var timestampRaw) ||
            !long.TryParse(timestampRaw.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp) ||
            !request.Headers.TryGetValue(PaymentRequestWebhookHeaderNames.KeyId, out var keyIdRaw) ||
            !request.Headers.TryGetValue(PaymentRequestWebhookHeaderNames.Signature, out var signatureRaw))
        {
            return false;
        }

        var keyId = keyIdRaw.ToString();
        var signature = signatureRaw.ToString();
        if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(signature))
            return false;

        headers = new PaymentRequestWebhookHeaders(eventId, timestamp, keyId, signature);
        return true;
    }

    private static async Task<byte[]> ReadBodyAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaximumBodyBytes)
            throw new InvalidOperationException("Webhook payload is too large.");

        await using var buffer = new MemoryStream();
        var rented = new byte[16 * 1024];
        var total = 0;

        while (true)
        {
            var read = await request.Body.ReadAsync(rented, cancellationToken);
            if (read == 0)
                break;

            total += read;
            if (total > MaximumBodyBytes)
                throw new InvalidOperationException("Webhook payload is too large.");

            await buffer.WriteAsync(rented.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }
}
