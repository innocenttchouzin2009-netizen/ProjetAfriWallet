using AfriWallet.Merchants.Intelligence.Domain.Metrics;
namespace AfriWallet.Merchants.Intelligence.Domain.Findings;
public sealed class MerchantRiskFinding
{
    public MerchantRiskFinding(Guid findingId, string merchantId, int score, MerchantRiskSeverity severity, MerchantProtectionRecommendation recommendation, MerchantCommerceMetrics metrics, IReadOnlyCollection<MerchantRiskPattern> patterns, DateTimeOffset createdAtUtc)
    {
        if (findingId == Guid.Empty) throw new ArgumentException("Finding id is required.", nameof(findingId));
        if (string.IsNullOrWhiteSpace(merchantId)) throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        FindingId = findingId; MerchantId = merchantId.Trim(); Score = Math.Clamp(score, 0, 100); Severity = severity; Recommendation = recommendation; Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics)); Patterns = patterns ?? throw new ArgumentNullException(nameof(patterns)); CreatedAtUtc = createdAtUtc;
    }
    public Guid FindingId { get; }
    public string MerchantId { get; }
    public int Score { get; }
    public MerchantRiskSeverity Severity { get; }
    public MerchantProtectionRecommendation Recommendation { get; }
    public MerchantCommerceMetrics Metrics { get; }
    public IReadOnlyCollection<MerchantRiskPattern> Patterns { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}
