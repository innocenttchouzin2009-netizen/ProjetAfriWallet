using System.Collections.Concurrent;
using AfriWallet.Fraud.Intelligence.Application.Abstractions;
using AfriWallet.Fraud.Intelligence.Domain.Findings;

namespace AfriWallet.Fraud.Intelligence.Infrastructure;

public sealed class InMemoryFraudIntelligenceRepository : IFraudIntelligenceRepository
{
    private readonly ConcurrentDictionary<string, IntelligenceFinding> items = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(IntelligenceFinding finding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items[finding.SubjectAwid] = finding;
        return Task.CompletedTask;
    }

    public Task<IntelligenceFinding?> GetLatestAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(awid, out var result);
        return Task.FromResult(result);
    }
}