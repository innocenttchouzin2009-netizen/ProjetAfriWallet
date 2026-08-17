using AfriWallet.Fraud.Intelligence.Domain.Findings;

namespace AfriWallet.Fraud.Intelligence.Application.Abstractions;

public interface IFraudIntelligenceRepository
{
    Task SaveAsync(IntelligenceFinding finding, CancellationToken cancellationToken = default);
    Task<IntelligenceFinding?> GetLatestAsync(string awid, CancellationToken cancellationToken = default);
}