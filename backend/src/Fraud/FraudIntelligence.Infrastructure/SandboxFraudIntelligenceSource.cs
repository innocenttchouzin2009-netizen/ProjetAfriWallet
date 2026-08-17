using AfriWallet.Fraud.Intelligence.Application.Abstractions;
using AfriWallet.Fraud.Intelligence.Application.Models;

namespace AfriWallet.Fraud.Intelligence.Infrastructure;

public sealed class SandboxFraudIntelligenceSource : IFraudIntelligenceSource
{
    private readonly Dictionary<string, IntelligenceSourceSnapshot> items = new(StringComparer.OrdinalIgnoreCase);

    public void Set(IntelligenceSourceSnapshot snapshot) => items[snapshot.Awid] = snapshot;

    public Task<IntelligenceSourceSnapshot?> GetAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(awid, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<IntelligenceSourceSnapshot>> GetNetworkAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<IntelligenceSourceSnapshot> result = items.Values.ToArray();
        return Task.FromResult(result);
    }
}