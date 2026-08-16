using System.Collections.Concurrent;
using AfriWallet.Compliance.Screening.Application.Abstractions;

namespace AfriWallet.Compliance.Screening.Infrastructure;

public sealed class InMemoryScreeningAuditStore : IScreeningAuditStore
{
    private readonly ConcurrentQueue<ScreeningAuditEvent> _events = new();

    public Task AppendAsync(
        ScreeningAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ScreeningAuditEvent>> GetBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ScreeningAuditEvent> result = _events
            .Where(auditEvent => auditEvent.SubjectId == subjectId)
            .ToArray();
        return Task.FromResult(result);
    }
}