using AfriWallet.PaymentPlatform.MobileMoney.Domain;

namespace AfriWallet.PaymentPlatform.MobileMoney.Application;

public sealed record InitiateMobileMoneyRequest(
    string PaymentIntentId,
    string ProviderCode,
    string Country,
    string Currency,
    string Msisdn,
    decimal Amount,
    string IdempotencyKey);

public sealed record ProviderPaymentRequest(
    Guid PaymentId,
    string PaymentIntentId,
    string Country,
    string Currency,
    string Msisdn,
    decimal Amount,
    string IdempotencyKey);

public sealed record ProviderPaymentResult(
    string ProviderReference,
    MobileMoneyPaymentStatus Status);

public sealed record ProviderStatusResult(
    string ProviderReference,
    MobileMoneyPaymentStatus Status);

public sealed record MobileMoneyCallback(
    string ProviderCode,
    string ProviderReference,
    string ExternalStatus,
    string? Signature);

public sealed record MobileMoneyAuditEvent(
    string EventName,
    Guid PaymentId,
    string ProviderCode,
    DateTimeOffset OccurredAt);

public sealed record MobileMoneyTelemetryEvent(
    string Metric,
    string ProviderCode,
    string Status,
    DateTimeOffset OccurredAt);