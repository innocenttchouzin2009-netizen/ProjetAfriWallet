using AfriWallet.Disputes.Resolution.Domain.Resolutions;

namespace AfriWallet.Disputes.Resolution.Application.Abstractions;

public interface IResolutionRepository
{
    Task AddAsync(ResolutionOrchestration resolution, CancellationToken cancellationToken = default);
    Task SaveAsync(ResolutionOrchestration resolution, CancellationToken cancellationToken = default);
    Task<ResolutionOrchestration?> GetAsync(Guid resolutionId, CancellationToken cancellationToken = default);
    Task<ResolutionOrchestration?> GetByDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default);
    Task<ResolutionOrchestration?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
