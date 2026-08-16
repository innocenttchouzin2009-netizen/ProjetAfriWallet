namespace AfriWallet.Compliance.RiskScoring.Application.Abstractions;

public interface IScreeningRiskSignalProvider
{
    Task<ScreeningRiskSignal> GetAsync(string awid, CancellationToken cancellationToken = default);
}

public sealed record ScreeningRiskSignal(
    bool HasMatch,
    bool HasBlockMatch,
    int HighestScore,
    int RiskScore,
    string Reason);