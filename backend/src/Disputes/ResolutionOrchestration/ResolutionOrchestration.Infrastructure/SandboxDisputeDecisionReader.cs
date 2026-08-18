using AfriWallet.Disputes.Resolution.Application.Abstractions;

namespace AfriWallet.Disputes.Resolution.Infrastructure;

public sealed class SandboxDisputeDecisionReader : IDisputeDecisionReader
{
    private readonly Dictionary<Guid, DisputeDecisionSnapshot> items = new();

    public void Set(DisputeDecisionSnapshot snapshot) => items[snapshot.DecisionId] = snapshot;

    public Task<DisputeDecisionSnapshot?> GetAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(decisionId, out var snapshot);
        return Task.FromResult(snapshot);
    }
}
