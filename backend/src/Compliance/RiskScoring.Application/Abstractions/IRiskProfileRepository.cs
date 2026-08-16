using AfriWallet.Compliance.RiskScoring.Domain.Profiles;

namespace AfriWallet.Compliance.RiskScoring.Application.Abstractions;

public interface IRiskProfileRepository
{
    Task SaveAsync(FinancialRiskProfile profile, CancellationToken cancellationToken = default);
    Task<FinancialRiskProfile?> GetLatestAsync(string awid, CancellationToken cancellationToken = default);
}