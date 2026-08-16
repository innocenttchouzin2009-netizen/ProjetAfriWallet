using AfriWallet.Compliance.RiskScoring.Application.Abstractions;

namespace AfriWallet.Compliance.RiskScoring.Infrastructure.Gateways;

public sealed class SandboxAmlRiskSignalProvider : IAmlRiskSignalProvider
{
    public Task<AmlRiskSignal> GetAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (awid.Contains("AML-HIGH", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AmlRiskSignal(
                OpenAlertCount: 3,
                HighestTransactionRisk: 85,
                RiskScore: 85,
                Reason: "sandbox high AML monitoring signal"));
        }

        return Task.FromResult(new AmlRiskSignal(
            OpenAlertCount: 0,
            HighestTransactionRisk: 10,
            RiskScore: 10,
            Reason: "sandbox AML risk low"));
    }
}