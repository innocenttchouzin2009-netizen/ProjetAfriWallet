using System.Collections.Concurrent;
using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Domain.Decisions;

namespace AfriWallet.Fraud.Decision.Infrastructure;

public sealed class InMemoryFraudDecisionRepository : IFraudDecisionRepository
{
    private readonly ConcurrentDictionary<Guid, FraudDecision> decisions = new();

    public Task SaveAsync(FraudDecision decision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        decisions[decision.TransactionId] = decision;
        return Task.CompletedTask;
    }

    public Task<FraudDecision?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        decisions.TryGetValue(transactionId, out var decision);
        return Task.FromResult(decision);
    }
}