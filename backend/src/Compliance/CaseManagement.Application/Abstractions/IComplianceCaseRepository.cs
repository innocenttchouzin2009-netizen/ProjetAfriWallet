using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Application.Abstractions;

public interface IComplianceCaseRepository
{
    Task AddAsync(ComplianceCase complianceCase, CancellationToken cancellationToken = default);
    Task SaveAsync(ComplianceCase complianceCase, CancellationToken cancellationToken = default);
    Task<ComplianceCase?> GetAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ComplianceCase>> GetByAwidAsync(string awid, CancellationToken cancellationToken = default);
}