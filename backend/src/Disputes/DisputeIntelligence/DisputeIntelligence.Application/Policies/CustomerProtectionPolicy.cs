using AfriWallet.Disputes.Intelligence.Domain.Findings;

namespace AfriWallet.Disputes.Intelligence.Application.Policies;

public sealed class CustomerProtectionPolicy
{
    public ProtectionSeverity ResolveSeverity(int score) =>
        score switch
        {
            >= 80 => ProtectionSeverity.Critical,
            >= 60 => ProtectionSeverity.High,
            >= 30 => ProtectionSeverity.Medium,
            >= 10 => ProtectionSeverity.Low,
            _ => ProtectionSeverity.Informational
        };

    public ProtectionRecommendation ResolveRecommendation(int score, bool merchantConcentration, bool failedResolutions)
    {
        if (score >= 80)
            return ProtectionRecommendation.EscalateOperations;
        if (merchantConcentration && score >= 50)
            return ProtectionRecommendation.ReviewMerchant;
        if (failedResolutions || score >= 30)
            return ProtectionRecommendation.CustomerProtectionReview;
        if (score >= 10)
            return ProtectionRecommendation.Monitor;
        return ProtectionRecommendation.NoAction;
    }
}
