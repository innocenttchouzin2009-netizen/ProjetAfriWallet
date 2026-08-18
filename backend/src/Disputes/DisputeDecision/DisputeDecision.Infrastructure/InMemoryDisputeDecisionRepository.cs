using System.Collections.Concurrent;
using AfriWallet.Disputes.Decision.Application.Abstractions;
using AfriWallet.Disputes.Decision.Domain.Decisions;

namespace AfriWallet.Disputes.Decision.Infrastructure;

public sealed class InMemoryDisputeDecisionRepository : IDisputeDecisionRepository
{
    private readonly ConcurrentDictionary<Guid, DisputeResolutionDecision> items = new();

    public Task AddAsync(DisputeResolutionDecision decision, CancellationToken cancellationToken = default)
    {
        if (!items.TryAdd(decision.DecisionId, decision))
            throw new InvalidOperationException("Decision already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(DisputeResolutionDecision decision, CancellationToken cancellationToken = default)
    {
        items[decision.DecisionId] = decision;
        return Task.CompletedTask;
    }

    public Task<DisputeResolutionDecision?> GetAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        items.TryGetValue(decisionId, out var result);
        return Task.FromResult(result);
    }

    public Task<DisputeResolutionDecision?> GetActiveByInvestigationAsync(Guid investigationId, CancellationToken cancellationToken = default)
    {
        var result = items.Values
            .Where(x => x.InvestigationId == investigationId)
            .Where(x => x.Status != ResolutionDecisionStatus.Superseded)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();
        return Task.FromResult(result);
    }
}
