using AfriWallet.Compliance.RiskScoring.Application.Abstractions;
using AfriWallet.Compliance.RiskScoring.Application.Policies;
using AfriWallet.Compliance.RiskScoring.Domain.Factors;
using AfriWallet.Compliance.RiskScoring.Domain.Profiles;
using AfriWallet.Compliance.RiskScoring.Domain.Scores;

namespace AfriWallet.Compliance.RiskScoring.Application.Scoring;

public sealed class FinancialRiskScoringService
{
    private readonly IKycRiskSignalProvider _kyc;
    private readonly IScreeningRiskSignalProvider _screening;
    private readonly IAmlRiskSignalProvider _aml;
    private readonly IRiskProfileRepository _profiles;
    private readonly IRiskAuditStore _audit;
    private readonly IRiskClock _clock;
    private readonly RiskScoringPolicy _policy;

    public FinancialRiskScoringService(
        IKycRiskSignalProvider kyc,
        IScreeningRiskSignalProvider screening,
        IAmlRiskSignalProvider aml,
        IRiskProfileRepository profiles,
        IRiskAuditStore audit,
        IRiskClock clock,
        RiskScoringPolicy policy)
    {
        _kyc = kyc;
        _screening = screening;
        _aml = aml;
        _profiles = profiles;
        _audit = audit;
        _clock = clock;
        _policy = policy;
    }

    public async Task<RiskScoringResult> CalculateAsync(
        CalculateRiskCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Awid))
            throw new ArgumentException("AWID is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var awid = command.Awid.Trim();
        var kycTask = _kyc.GetAsync(awid, cancellationToken);
        var screeningTask = _screening.GetAsync(awid, cancellationToken);
        var amlTask = _aml.GetAsync(awid, cancellationToken);
        await Task.WhenAll(kycTask, screeningTask, amlTask);

        var kyc = await kycTask;
        var screening = await screeningTask;
        var aml = await amlTask;
        var contributions = new[]
        {
            CreateContribution("RISK-KYC", RiskFactorType.Kyc, kyc.RiskScore, _policy.KycWeight, kyc.Reason),
            CreateContribution("RISK-SCREENING", RiskFactorType.SanctionsPep, screening.RiskScore, _policy.ScreeningWeight, screening.Reason),
            CreateContribution("RISK-AML", RiskFactorType.AmlMonitoring, aml.RiskScore, _policy.AmlWeight, aml.Reason)
        };
        var totalWeight = contributions.Sum(contribution => contribution.Weight);
        if (totalWeight <= 0)
            throw new InvalidOperationException("Risk scoring policy must define a positive total weight.");

        var finalScore = RiskScore.Create(
            (int)Math.Round(
                contributions.Sum(contribution => contribution.WeightedScore) / (double)totalWeight,
                MidpointRounding.AwayFromZero));
        var band = _policy.ResolveBand(finalScore.Value);
        var decision = _policy.ResolveDecision(finalScore.Value, screening.HasBlockMatch);
        var profile = new FinancialRiskProfile(
            Guid.NewGuid(),
            awid,
            finalScore,
            band,
            decision,
            contributions,
            _clock.UtcNow);

        await _profiles.SaveAsync(profile, cancellationToken);
        await _audit.AppendAsync(
            new RiskAuditEvent(
                Guid.NewGuid(),
                profile.Awid,
                profile.ProfileId,
                "risk.score.calculated",
                command.Actor,
                _clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["score"] = finalScore.Value.ToString(),
                    ["band"] = band.ToString(),
                    ["decision"] = decision.ToString()
                }),
            cancellationToken);

        return new RiskScoringResult(
            profile.ProfileId,
            profile.Awid,
            profile.Score.Value,
            profile.Band,
            profile.Decision,
            profile.Contributions,
            profile.CalculatedAtUtc);
    }

    private static RiskFactorContribution CreateContribution(
        string code,
        RiskFactorType type,
        int rawScore,
        int weight,
        string reason)
    {
        var normalized = Math.Clamp(rawScore, 0, 100);
        var weighted = checked(normalized * weight);
        return new RiskFactorContribution(code, type, normalized, weight, weighted, reason);
    }
}