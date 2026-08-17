using AfriWallet.Fraud.Investigation.Domain.Cases;

namespace AfriWallet.Fraud.Investigation.Application.Abstractions;

public interface IFraudCaseRepository
{
    Task AddAsync(FraudCase fraudCase, CancellationToken cancellationToken = default);
    Task SaveAsync(FraudCase fraudCase, CancellationToken cancellationToken = default);
    Task<FraudCase?> GetAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FraudCase>> GetByAwidAsync(string awid, CancellationToken cancellationToken = default);
}