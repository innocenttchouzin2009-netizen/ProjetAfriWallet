using BankRouting.Application.Interfaces;
using BankRouting.Domain.Routing;

namespace BankRouting.Infrastructure.Repositories;

public sealed class InMemoryBankRoutingDecisionRepository : IBankRoutingDecisionRepository
{
    private readonly Dictionary<Guid, RoutingDecision> _byDecisionId = new();
    private readonly Dictionary<Guid, RoutingDecision> _byTransferIntentId = new();
    private readonly Dictionary<string, RoutingDecision> _byIdempotencyKey = new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(RoutingDecision decision, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _byDecisionId[decision.DecisionId] = decision;
        _byTransferIntentId[decision.TransferIntentId] = decision;
        _byIdempotencyKey[decision.IdempotencyKey] = decision;

        return Task.CompletedTask;
    }

    public Task<RoutingDecision?> GetAsync(Guid decisionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_byDecisionId.TryGetValue(decisionId, out var decision) ? decision : null);
    }

    public Task<RoutingDecision?> GetByTransferIntentAsync(Guid transferIntentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_byTransferIntentId.TryGetValue(transferIntentId, out var decision) ? decision : null);
    }

    public Task<RoutingDecision?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_byIdempotencyKey.TryGetValue(idempotencyKey, out var decision) ? decision : null);
    }
}
