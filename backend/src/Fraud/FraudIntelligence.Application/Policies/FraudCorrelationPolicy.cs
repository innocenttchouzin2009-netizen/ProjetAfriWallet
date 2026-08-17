using AfriWallet.Fraud.Intelligence.Domain.Findings;

namespace AfriWallet.Fraud.Intelligence.Application.Policies;

public sealed class FraudCorrelationPolicy
{
    public IntelligenceSeverity ResolveSeverity(int score) => score switch
    {
        >= 80 => IntelligenceSeverity.Critical,
        >= 60 => IntelligenceSeverity.High,
        >= 30 => IntelligenceSeverity.Medium,
        >= 10 => IntelligenceSeverity.Low,
        _ => IntelligenceSeverity.Informational
    };
}