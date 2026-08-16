using System.Collections.Concurrent;
using AfriWallet.Compliance.RiskScoring.Application.Abstractions;
using AfriWallet.Compliance.RiskScoring.Domain.Profiles;

namespace AfriWallet.Compliance.RiskScoring.Infrastructure;

public sealed class InMemoryRiskProfileRepository : IRiskProfileRepository
{
    private readonly ConcurrentDictionary<string, FinancialRiskProfile> _profiles =
        new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(FinancialRiskProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _profiles[profile.Awid] = profile;
        return Task.CompletedTask;
    }

    public Task<FinancialRiskProfile?> GetLatestAsync(
        string awid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _profiles.TryGetValue(awid, out var profile);
        return Task.FromResult(profile);
    }
}