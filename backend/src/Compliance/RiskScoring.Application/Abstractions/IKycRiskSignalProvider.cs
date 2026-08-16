namespace AfriWallet.Compliance.RiskScoring.Application.Abstractions;

public interface IKycRiskSignalProvider
{
    Task<KycRiskSignal> GetAsync(string awid, CancellationToken cancellationToken = default);
}

public sealed record KycRiskSignal(
    bool ProfileExists,
    bool Verified,
    string KycLevel,
    int RiskScore,
    string Reason);