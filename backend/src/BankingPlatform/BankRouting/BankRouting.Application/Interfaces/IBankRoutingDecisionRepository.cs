using BankRouting.Domain.Routing;

namespace BankRouting.Application.Interfaces;

public interface IBankRoutingDecisionRepository
{
    Task AddAsync(RoutingDecision decision, CancellationToken cancellationToken);
    Task<RoutingDecision?> GetByTransferIntentAsync(Guid transferIntentId, CancellationToken cancellationToken);
    Task<RoutingDecision?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<RoutingDecision?> GetAsync(Guid decisionId, CancellationToken cancellationToken);
}
