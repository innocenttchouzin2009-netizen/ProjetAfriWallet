using AfriWallet.Compliance.RiskScoring.Application.Abstractions;

namespace AfriWallet.Compliance.RiskScoring.Infrastructure.Gateways;

public sealed class SandboxKycRiskSignalProvider : IKycRiskSignalProvider
{
    public Task<KycRiskSignal> GetAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (awid.Contains("UNVERIFIED", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new KycRiskSignal(
                ProfileExists: true,
                Verified: false,
                KycLevel: "Basic",
                RiskScore: 70,
                Reason: "sandbox KYC not verified"));
        }

        return Task.FromResult(new KycRiskSignal(
            ProfileExists: true,
            Verified: true,
            KycLevel: "Standard",
            RiskScore: 10,
            Reason: "sandbox KYC verified"));
    }
}