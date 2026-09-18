namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestWebhookSigningRequest(
    PaymentRequestEventDispatch Dispatch,
    DateTimeOffset SignedAtUtc,
    ReadOnlyMemory<byte> Secret);

public sealed record PaymentRequestWebhookSignature(
    string Version,
    long TimestampUnixSeconds,
    string DigestHex)
{
    public string HeaderValue => $"{Version}={DigestHex}";
}

public interface IPaymentRequestWebhookSigner
{
    PaymentRequestWebhookSignature Sign(PaymentRequestWebhookSigningRequest request);

    bool Verify(
        PaymentRequestWebhookSigningRequest request,
        PaymentRequestWebhookSignature signature);
}
