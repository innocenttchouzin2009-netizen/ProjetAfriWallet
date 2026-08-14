using ComplianceProfileDomain = AfriWallet.CompliancePlatform.ComplianceProfile.Domain.ComplianceProfile;

namespace AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;

public interface IComplianceProfileRepository
{
    Task AddAsync(ComplianceProfileDomain profile, CancellationToken cancellationToken);
    Task<ComplianceProfileDomain?> GetAsync(Guid profileId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ComplianceProfileDomain>> ListByCustomerAsync(string customerId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ComplianceProfileDomain>> ListAsync(CancellationToken cancellationToken);
}
