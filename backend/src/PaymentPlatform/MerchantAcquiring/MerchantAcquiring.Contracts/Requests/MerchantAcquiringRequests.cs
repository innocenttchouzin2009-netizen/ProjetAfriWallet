using MerchantAcquiring.Domain.Profiles;

namespace MerchantAcquiring.Contracts.Requests;

public sealed record CreateAcquiringProfileRequest(
    string MerchantId,
    string CountryCode,
    string SettlementCurrency);

public sealed record ConfigureAcquiringProfileRequest(
    IReadOnlyCollection<string> Currencies,
    IReadOnlyCollection<AcquiringPaymentMethod> Methods,
    decimal PercentageFee,
    long FixedFeeMinor);

public sealed record CreateMerchantPaymentRequest(
    Guid PaymentIntentId,
    string MerchantId,
    string CurrencyCode,
    long AmountMinor,
    AcquiringPaymentMethod PaymentMethod,
    string IdempotencyKey);

public sealed record AuthorizeMerchantPaymentRequest(
    string CountryCode);

public sealed record RefundMerchantPaymentRequest(
    long AmountMinor,
    string Reason);
