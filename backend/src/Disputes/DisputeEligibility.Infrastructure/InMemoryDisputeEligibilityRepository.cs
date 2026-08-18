using System.Collections.Concurrent;
using AfriWallet.Disputes.Eligibility.Application.Abstractions;
using AfriWallet.Disputes.Eligibility.Domain.Eligibility;

namespace AfriWallet.Disputes.Eligibility.Infrastructure;

public sealed class InMemoryDisputeEligibilityRepository : IDisputeEligibilityRepository
{
    private readonly ConcurrentDictionary<Guid, DisputeEligibilityDecision> items = new();

    public Task SaveAsync(DisputeEligibilityDecision decision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items[decision.ClaimId] = decision;
        return Task.CompletedTask;
    }

    public Task<DisputeEligibilityDecision?> GetByClaimAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(claimId, out var result);
        return Task.FromResult(result);
    }
}
