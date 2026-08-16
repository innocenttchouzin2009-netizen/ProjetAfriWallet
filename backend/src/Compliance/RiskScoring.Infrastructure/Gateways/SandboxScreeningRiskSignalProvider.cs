using AfriWallet.Compliance.RiskScoring.Application.Abstractions;

namespace AfriWallet.Compliance.RiskScoring.Infrastructure.Gateways;

public sealed class SandboxScreeningRiskSignalProvider : IScreeningRiskSignalProvider
{
    public Task<ScreeningRiskSignal> GetAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (awid.Contains("BLOCK", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ScreeningRiskSignal(
                HasMatch: true,
                HasBlockMatch: true,
                HighestScore: 98,
                RiskScore: 100,
                Reason: "sandbox sanctions block match"));
        }

        if (awid.Contains("PEP", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ScreeningRiskSignal(
                HasMatch: true,
                HasBlockMatch: false,
                HighestScore: 80,
                RiskScore: 65,
                Reason: "sandbox PEP review signal"));
        }

        return Task.FromResult(new ScreeningRiskSignal(
            HasMatch: false,
            HasBlockMatch: false,
            HighestScore: 0,
            RiskScore: 0,
            Reason: "no sandbox screening match"));
    }
}