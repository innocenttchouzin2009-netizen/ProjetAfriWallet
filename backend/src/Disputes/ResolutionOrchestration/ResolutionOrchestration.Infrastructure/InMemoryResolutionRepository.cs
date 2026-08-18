using System.Collections.Concurrent;
using AfriWallet.Disputes.Resolution.Application.Abstractions;
using AfriWallet.Disputes.Resolution.Domain.Resolutions;

namespace AfriWallet.Disputes.Resolution.Infrastructure;

public sealed class InMemoryResolutionRepository : IResolutionRepository
{
    private readonly ConcurrentDictionary<Guid, ResolutionOrchestration> items = new();

    public Task AddAsync(ResolutionOrchestration resolution, CancellationToken cancellationToken = default)
    {
        if (!items.TryAdd(resolution.ResolutionId, resolution))
            throw new InvalidOperationException("Resolution already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(ResolutionOrchestration resolution, CancellationToken cancellationToken = default)
    {
        items[resolution.ResolutionId] = resolution;
        return Task.CompletedTask;
    }

    public Task<ResolutionOrchestration?> GetAsync(Guid resolutionId, CancellationToken cancellationToken = default)
    {
        items.TryGetValue(resolutionId, out var result);
        return Task.FromResult(result);
    }

    public Task<ResolutionOrchestration?> GetByDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        var result = items.Values.FirstOrDefault(x => x.DecisionId == decisionId);
        return Task.FromResult(result);
    }

    public Task<ResolutionOrchestration?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var result = items.Values.FirstOrDefault(x => string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
        return Task.FromResult(result);
    }
}
