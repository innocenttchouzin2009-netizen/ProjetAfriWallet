using AfriWallet.Disputes.Intelligence.Application.Abstractions;
using AfriWallet.Disputes.Intelligence.Application.Models;

namespace AfriWallet.Disputes.Intelligence.Infrastructure;

public sealed class SandboxDisputeIntelligenceSource : IDisputeIntelligenceSource
{
    private readonly Dictionary<string, DisputeIntelligenceSnapshot> items = new(StringComparer.OrdinalIgnoreCase);

    public void Set(DisputeIntelligenceSnapshot snapshot) => items[snapshot.SubjectId] = snapshot;

    public Task<DisputeIntelligenceSnapshot?> GetAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(subjectId, out var result);
        return Task.FromResult(result);
    }
}
