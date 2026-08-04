using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Risk;
using UniversalWallet.Api.Payments.Application.Intents;

namespace UniversalWallet.Api.Payments.Infrastructure.Risk;

public sealed class DefaultRiskEngine : IPaymentRiskEngine
{
    public RiskAssessment Assess(PaymentIntent intent, PaymentWalletSnapshot wallet, string deviceId, string sessionId)
    {
        var score = 0;
        if (string.IsNullOrWhiteSpace(deviceId)) score += 25;
        if (string.IsNullOrWhiteSpace(sessionId)) score += 25;
        if (intent.AmountMinor > 50000) score += 15;
        if (intent.RecipientType == RecipientType.Merchant) score += 10;
        if (intent.AmountMinor > 100000) score += 25;

        var level = score switch
        {
            >= 80 => RiskLevel.Critical,
            >= 50 => RiskLevel.High,
            >= 25 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };

        var action = level switch
        {
            RiskLevel.Critical => RecommendedRiskAction.Block,
            RiskLevel.High => RecommendedRiskAction.Review,
            RiskLevel.Medium => RecommendedRiskAction.StepUp,
            _ => RecommendedRiskAction.Allow
        };

        return new RiskAssessment
        {
            Score = Math.Clamp(score, 0, 100),
            Level = level,
            RecommendedAction = action,
            RulesVersion = "risk-v2"
        };
    }
}
