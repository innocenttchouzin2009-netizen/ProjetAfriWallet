using AfriWallet.Compliance.RiskScoring.Domain.Factors;
using AfriWallet.Compliance.RiskScoring.Domain.Profiles;

namespace AfriWallet.Compliance.RiskScoring.Application.Scoring;

public sealed record RiskScoringResult(
    Guid RiskProfileId,
    string Awid,
    int Score,
    RiskBand Band,
    RiskDecision Decision,
    IReadOnlyCollection<RiskFactorContribution> Contributions,
    DateTimeOffset CalculatedAtUtc);