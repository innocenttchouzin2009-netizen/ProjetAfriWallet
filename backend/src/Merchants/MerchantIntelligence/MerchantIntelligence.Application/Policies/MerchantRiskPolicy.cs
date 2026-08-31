using AfriWallet.Merchants.Intelligence.Domain.Findings;
namespace AfriWallet.Merchants.Intelligence.Application.Policies;
public sealed class MerchantRiskPolicy
{
    public MerchantRiskSeverity ResolveSeverity(int score) => score switch { >= 80 => MerchantRiskSeverity.Critical, >= 60 => MerchantRiskSeverity.High, >= 30 => MerchantRiskSeverity.Medium, >= 10 => MerchantRiskSeverity.Low, _ => MerchantRiskSeverity.Informational };
    public MerchantProtectionRecommendation ResolveRecommendation(int score, bool settlementRisk, bool customerProtectionRisk)
    {
        if (score >= 80) return MerchantProtectionRecommendation.EscalateOperations;
        if (settlementRisk && score >= 50) return MerchantProtectionRecommendation.ReviewSettlementActivity;
        if (customerProtectionRisk && score >= 40) return MerchantProtectionRecommendation.CustomerProtectionReview;
        if (score >= 30) return MerchantProtectionRecommendation.ReviewMerchant;
        if (score >= 10) return MerchantProtectionRecommendation.Monitor;
        return MerchantProtectionRecommendation.NoAction;
    }
}
