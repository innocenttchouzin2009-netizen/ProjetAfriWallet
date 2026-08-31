namespace AfriWallet.Merchants.Intelligence.Application.Models;
public sealed record MerchantProfileSnapshot(string MerchantId, string RegistryStatus, string VerificationStatus, string CountryCode, string SettlementCurrency);
public sealed record CheckoutMetricSnapshot(Guid CheckoutSessionId, string Status, long AmountMinor, string Currency);
public sealed record PaymentDecisionMetricSnapshot(Guid PaymentIntentId, string DecisionType, string DecisionStatus, int RiskScore);
public sealed record SettlementMetricSnapshot(Guid SettlementId, string Route, string Status, int AttemptCount);
public sealed record DisputeMetricSnapshot(Guid ClaimId, string Classification, string DecisionType, string ResolutionStatus);
public sealed record MerchantIntelligenceSnapshot(string MerchantId, MerchantProfileSnapshot Merchant, IReadOnlyCollection<CheckoutMetricSnapshot> Checkouts, IReadOnlyCollection<PaymentDecisionMetricSnapshot> Decisions, IReadOnlyCollection<SettlementMetricSnapshot> Settlements, IReadOnlyCollection<DisputeMetricSnapshot> Disputes);
