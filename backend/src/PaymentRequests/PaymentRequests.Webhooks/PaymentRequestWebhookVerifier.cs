using System.Security.Cryptography;

namespace AfriWallet.PaymentRequests.Webhooks;

public sealed class PaymentRequestWebhookVerifier(
    IPaymentRequestWebhookSecretProvider secretProvider,
    IPaymentRequestWebhookReplayGuard replayGuard,
    TimeProvider timeProvider,
    PaymentRequestWebhookSecurityOptions? options = null)
{
    private readonly PaymentRequestWebhookSecurityOptions options =
        options ?? PaymentRequestWebhookSecurityOptions.Default;

    public PaymentRequestWebhookVerificationResult Verify(
        PaymentRequestWebhookVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        this.options.Validate();

        var headers = request.Headers;
        if (headers.EventId == Guid.Empty ||
            !RotatingPaymentRequestWebhookSecretProvider.IsValidKeyId(headers.KeyId))
            return PaymentRequestWebhookVerificationResult.InvalidRequest();

        DateTimeOffset signedAtUtc;
        try
        {
            signedAtUtc = DateTimeOffset.FromUnixTimeSeconds(headers.TimestampUnixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return PaymentRequestWebhookVerificationResult.InvalidRequest();
        }

        var nowUtc = timeProvider.GetUtcNow();
        var age = nowUtc - signedAtUtc;
        if (age.Duration() > this.options.MaximumClockSkew)
            return PaymentRequestWebhookVerificationResult.TimestampOutsideTolerance();

        var secret = secretProvider.GetVerificationSecret(headers.KeyId, signedAtUtc, nowUtc);
        if (secret is null)
            return PaymentRequestWebhookVerificationResult.UnknownOrInactiveKey();

        if (!PaymentRequestWebhookCryptography.TryDecodeSignature(headers.Signature, out var supplied))
            return PaymentRequestWebhookVerificationResult.InvalidSignature();

        var calculated = PaymentRequestWebhookCryptography.ComputeSignature(
            request.Payload.Span,
            headers.EventId,
            headers.TimestampUnixSeconds,
            headers.KeyId,
            secret.Secret);

        try
        {
            if (!CryptographicOperations.FixedTimeEquals(calculated, supplied))
                return PaymentRequestWebhookVerificationResult.InvalidSignature();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(calculated);
            CryptographicOperations.ZeroMemory(supplied);
        }

        var retainUntilUtc = nowUtc.Add(this.options.ReplayWindow);
        if (!replayGuard.TryAccept(headers.EventId, nowUtc, retainUntilUtc))
            return PaymentRequestWebhookVerificationResult.ReplayDetected();

        return PaymentRequestWebhookVerificationResult.Verified();
    }
}
