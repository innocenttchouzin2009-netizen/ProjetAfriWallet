namespace AfriWallet.Compliance.RiskScoring.Application.Abstractions;

public interface IAmlRiskSignalProvider
{
    Task<AmlRiskSignal> GetAsync(string awid, CancellationToken cancellationToken = default);
}

public sealed record AmlRiskSignal(
    int OpenAlertCount,
    int HighestTransactionRisk,
    int RiskScore,
    string Reason);