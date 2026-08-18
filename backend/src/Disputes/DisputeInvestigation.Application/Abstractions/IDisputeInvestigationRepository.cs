using AfriWallet.Disputes.Investigation.Domain.Cases;

namespace AfriWallet.Disputes.Investigation.Application.Abstractions;

public interface IDisputeInvestigationRepository
{
    Task AddAsync(DisputeInvestigationCase investigation, CancellationToken cancellationToken = default);
    Task SaveAsync(DisputeInvestigationCase investigation, CancellationToken cancellationToken = default);
    Task<DisputeInvestigationCase?> GetAsync(Guid investigationId, CancellationToken cancellationToken = default);
}
