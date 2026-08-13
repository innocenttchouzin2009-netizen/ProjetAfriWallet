namespace AfriWallet.PaymentPlatform.ProviderIntegration.Domain;

public sealed record ProviderWebhookEvent(
    string ProviderCode,
    string EventId,
    string ProviderReference,
    string EventType,
    string Payload,
    string Signature,
    DateTimeOffset ReceivedAt);