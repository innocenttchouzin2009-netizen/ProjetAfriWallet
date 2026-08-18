using System.Collections.Concurrent;
using AfriWallet.Disputes.Intelligence.Application.Abstractions;
using AfriWallet.Disputes.Intelligence.Domain.Findings;

namespace AfriWallet.Disputes.Intelligence.Infrastructure;

public sealed class InMemoryDisputeIntelligenceRepository : IDisputeIntelligenceRepository
{
    private readonly ConcurrentDictionary<string, ProtectionFinding> items = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(ProtectionFinding finding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items[finding.SubjectId] = finding;
        return Task.CompletedTask;
    }

    public Task<ProtectionFinding?> GetLatestAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(subjectId, out var result);
        return Task.FromResult(result);
    }
}
