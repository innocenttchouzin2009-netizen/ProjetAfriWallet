using System.Collections.Concurrent;
using AfriWallet.Compliance.CaseManagement.Application.Abstractions;
using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Infrastructure;

public sealed class InMemoryComplianceCaseRepository : IComplianceCaseRepository
{
    private readonly ConcurrentDictionary<Guid, ComplianceCase> _cases = new();
    public Task AddAsync(ComplianceCase item, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); if (!_cases.TryAdd(item.CaseId, item)) throw new InvalidOperationException("Compliance case already exists."); return Task.CompletedTask; }
    public Task SaveAsync(ComplianceCase item, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); _cases[item.CaseId] = item; return Task.CompletedTask; }
    public Task<ComplianceCase?> GetAsync(Guid id, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); _cases.TryGetValue(id, out var item); return Task.FromResult(item); }
    public Task<IReadOnlyCollection<ComplianceCase>> GetByAwidAsync(string awid, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); IReadOnlyCollection<ComplianceCase> result = _cases.Values.Where(x => string.Equals(x.Awid, awid, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.CreatedAtUtc).ToArray(); return Task.FromResult(result); }
}