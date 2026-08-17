using System.Collections.Concurrent;
using AfriWallet.Fraud.Investigation.Application.Abstractions;
using AfriWallet.Fraud.Investigation.Domain.Cases;

namespace AfriWallet.Fraud.Investigation.Infrastructure;

public sealed class InMemoryFraudCaseRepository : IFraudCaseRepository
{
    private readonly ConcurrentDictionary<Guid, FraudCase> cases = new();

    public Task AddAsync(FraudCase fraudCase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!cases.TryAdd(fraudCase.CaseId, fraudCase)) throw new InvalidOperationException("Fraud case already exists.");
        return Task.CompletedTask;
    }

    public Task SaveAsync(FraudCase fraudCase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cases[fraudCase.CaseId] = fraudCase;
        return Task.CompletedTask;
    }

    public Task<FraudCase?> GetAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cases.TryGetValue(caseId, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<FraudCase>> GetByAwidAsync(string awid, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<FraudCase> result = cases.Values.Where(x => string.Equals(x.Awid, awid, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.CreatedAtUtc).ToArray();
        return Task.FromResult(result);
    }
}