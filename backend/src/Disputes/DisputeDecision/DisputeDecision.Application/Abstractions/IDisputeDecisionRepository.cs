using AfriWallet.Disputes.Decision.Domain.Decisions;

namespace AfriWallet.Disputes.Decision.Application.Abstractions;

public interface IDisputeDecisionRepository
{
    Task AddAsync(DisputeResolutionDecision decision, CancellationToken cancellationToken = default);
    Task SaveAsync(DisputeResolutionDecision decision, CancellationToken cancellationToken = default);
    Task<DisputeResolutionDecision?> GetAsync(Guid decisionId, CancellationToken cancellationToken = default);
    Task<DisputeResolutionDecision?> GetActiveByInvestigationAsync(Guid investigationId, CancellationToken cancellationToken = default);
}
