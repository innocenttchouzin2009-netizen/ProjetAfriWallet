using AfriWallet.Compliance.RiskScoring.Domain.Profiles;

namespace AfriWallet.Compliance.RiskScoring.Application.Policies;

public sealed class RiskScoringPolicy
{
    public int KycWeight { get; init; } = 30;
    public int ScreeningWeight { get; init; } = 40;
    public int AmlWeight { get; init; } = 30;

    public RiskBand ResolveBand(int score) => score switch
    {
        >= 80 => RiskBand.Critical,
        >= 60 => RiskBand.High,
        >= 30 => RiskBand.Medium,
        _ => RiskBand.Low
    };

    public RiskDecision ResolveDecision(int score, bool sanctionsBlock) =>
        sanctionsBlock
            ? RiskDecision.Restrict
            : score switch
            {
                >= 80 => RiskDecision.Restrict,
                >= 40 => RiskDecision.Review,
                _ => RiskDecision.Allow
            };
}