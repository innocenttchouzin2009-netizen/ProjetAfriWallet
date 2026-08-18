using System.Collections.Concurrent;
using AfriWallet.Disputes.Investigation.Application.Abstractions;
using AfriWallet.Disputes.Investigation.Domain.Cases;

namespace AfriWallet.Disputes.Investigation.Infrastructure;

public sealed class InMemoryDisputeInvestigationRepository : IDisputeInvestigationRepository
{
    private readonly ConcurrentDictionary<Guid, DisputeInvestigationCase> items = new();

    public Task AddAsync(DisputeInvestigationCase investigation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!items.TryAdd(investigation.InvestigationId, investigation))
            throw new InvalidOperationException("Investigation already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(DisputeInvestigationCase investigation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items[investigation.InvestigationId] = investigation;
        return Task.CompletedTask;
    }

    public Task<DisputeInvestigationCase?> GetAsync(Guid investigationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items.TryGetValue(investigationId, out var result);
        return Task.FromResult(result);
    }
}
