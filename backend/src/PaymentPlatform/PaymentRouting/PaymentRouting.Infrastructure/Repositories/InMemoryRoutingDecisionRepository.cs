using System.Collections.Concurrent;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Domain.Decisions;

namespace PaymentRouting.Infrastructure.Repositories;

public sealed class InMemoryRoutingDecisionRepository :
    IRoutingDecisionRepository
{
    private readonly ConcurrentDictionary<
        Guid,
        RoutingDecision> _decisions = new();

    public Task AddAsync(
        RoutingDecision decision,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_decisions.TryAdd(
                decision.PaymentIntentId,
                decision))
        {
            throw new InvalidOperationException(
                "Routing decision already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<RoutingDecision?>
        GetByPaymentIntentAsync(
            Guid paymentIntentId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _decisions.TryGetValue(
            paymentIntentId,
            out var decision);

        return Task.FromResult(decision);
    }
}
