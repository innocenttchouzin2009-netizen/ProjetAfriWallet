using AfriWallet.Compliance.RiskScoring.Domain.Factors;
using AfriWallet.Compliance.RiskScoring.Domain.Scores;

namespace AfriWallet.Compliance.RiskScoring.Domain.Profiles;

public sealed class FinancialRiskProfile
{
    public FinancialRiskProfile(
        Guid profileId,
        string awid,
        RiskScore score,
        RiskBand band,
        RiskDecision decision,
        IReadOnlyCollection<RiskFactorContribution> contributions,
        DateTimeOffset calculatedAtUtc)
    {
        if (profileId == Guid.Empty)
            throw new ArgumentException("Risk profile id is required.");
        if (string.IsNullOrWhiteSpace(awid))
            throw new ArgumentException("AWID is required.");

        ProfileId = profileId;
        Awid = awid.Trim();
        Score = score;
        Band = band;
        Decision = decision;
        Contributions = contributions;
        CalculatedAtUtc = calculatedAtUtc;
    }

    public Guid ProfileId { get; }
    public string Awid { get; }
    public RiskScore Score { get; }
    public RiskBand Band { get; }
    public RiskDecision Decision { get; }
    public IReadOnlyCollection<RiskFactorContribution> Contributions { get; }
    public DateTimeOffset CalculatedAtUtc { get; }
}