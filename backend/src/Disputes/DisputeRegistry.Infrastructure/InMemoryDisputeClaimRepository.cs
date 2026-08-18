using System.Collections.Concurrent;
using AfriWallet.Disputes.Registry.Application.Abstractions;
using AfriWallet.Disputes.Registry.Domain.Claims;

namespace AfriWallet.Disputes.Registry.Infrastructure;

public sealed class InMemoryDisputeClaimRepository : IDisputeClaimRepository
{
    private readonly ConcurrentDictionary<Guid, DisputeClaim> claims = new();

    public Task AddAsync(DisputeClaim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!claims.TryAdd(claim.ClaimId, claim))
            throw new InvalidOperationException("Dispute claim already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(DisputeClaim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        claims[claim.ClaimId] = claim;
        return Task.CompletedTask;
    }

    public Task<DisputeClaim?> GetAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        claims.TryGetValue(claimId, out var claim);
        return Task.FromResult(claim);
    }

    public Task<IReadOnlyCollection<DisputeClaim>> GetByAwidAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<DisputeClaim> result = claims.Values
            .Where(x => string.Equals(x.Awid, awid, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();
        return Task.FromResult(result);
    }
}
