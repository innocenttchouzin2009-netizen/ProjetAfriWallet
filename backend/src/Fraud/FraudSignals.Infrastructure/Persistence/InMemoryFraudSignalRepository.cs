using System.Collections.Concurrent;using AfriWallet.Fraud.Signals.Application.Abstractions;using AfriWallet.Fraud.Signals.Domain.Aggregates;
namespace AfriWallet.Fraud.Signals.Infrastructure.Persistence;
public sealed class InMemoryFraudSignalRepository:IFraudSignalRepository
{
    private readonly ConcurrentDictionary<Guid,FraudSignal> _signals=new();private readonly ConcurrentDictionary<string,Guid> _eventIds=new(StringComparer.OrdinalIgnoreCase);
    public Task<bool> ExistsByEventIdAsync(string eventId,CancellationToken ct=default){ct.ThrowIfCancellationRequested();return Task.FromResult(_eventIds.ContainsKey(eventId));}
    public Task AddAsync(FraudSignal signal,CancellationToken ct=default){ct.ThrowIfCancellationRequested();if(!_eventIds.TryAdd(signal.EventId,signal.Id))throw new InvalidOperationException($"Duplicate event id: {signal.EventId}");if(!_signals.TryAdd(signal.Id,signal)){_eventIds.TryRemove(signal.EventId,out _);throw new InvalidOperationException($"Duplicate signal id: {signal.Id}");}return Task.CompletedTask;}
    public Task<FraudSignal?> GetAsync(Guid id,CancellationToken ct=default){ct.ThrowIfCancellationRequested();_signals.TryGetValue(id,out var signal);return Task.FromResult(signal);}
    public Task<IReadOnlyCollection<FraudSignal>> GetBySubjectAsync(string type,string id,CancellationToken ct=default){ct.ThrowIfCancellationRequested();IReadOnlyCollection<FraudSignal> result=_signals.Values.Where(x=>string.Equals(x.Subject.Type,type,StringComparison.OrdinalIgnoreCase)&&string.Equals(x.Subject.Id,id,StringComparison.Ordinal)).OrderBy(x=>x.OccurredAt).ToArray();return Task.FromResult(result);}
}