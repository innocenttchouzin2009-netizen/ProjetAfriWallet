using AfriWallet.Merchants.Intelligence.Domain.Findings;
using AfriWallet.Merchants.Intelligence.Domain.Metrics;
namespace AfriWallet.Merchants.Intelligence.Application.Services;
public sealed record EvaluateMerchantRiskCommand(string MerchantId, string Actor);
public sealed record MerchantRiskResult(Guid FindingId, string MerchantId, int Score, MerchantRiskSeverity Severity, MerchantProtectionRecommendation Recommendation, MerchantCommerceMetrics Metrics, IReadOnlyCollection<MerchantRiskPattern> Patterns, DateTimeOffset CreatedAtUtc);
