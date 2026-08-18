using AfriWallet.Disputes.Investigation.Application.Abstractions;

namespace AfriWallet.Disputes.Investigation.Infrastructure;

public sealed class SandboxDisputeEligibilityReader : IDisputeEligibilityReader
{
    private readonly Dictionary<Guid, DisputeEligibilitySnapshot> items = new();

    public void Set(DisputeEligibilitySnapshot snapshot) => items[snapshot.ClaimId] = snapshot;

    public Task<DisputeEligibilitySnapshot?> GetByClaimAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(claimId, out var result);
        return Task.FromResult(result);
    }
}
