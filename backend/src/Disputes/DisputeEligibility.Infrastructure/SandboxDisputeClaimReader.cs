using AfriWallet.Disputes.Eligibility.Application.Abstractions;
using AfriWallet.Disputes.Eligibility.Domain.Claims;

namespace AfriWallet.Disputes.Eligibility.Infrastructure;

public sealed class SandboxDisputeClaimReader : IDisputeClaimReader
{
    private readonly Dictionary<Guid, DisputeClaimSnapshot> items = new();

    public void Set(DisputeClaimSnapshot claim) => items[claim.ClaimId] = claim;

    public Task<DisputeClaimSnapshot?> GetAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(claimId, out var claim);
        return Task.FromResult(claim);
    }
}
