namespace AfriWallet.Compliance.Screening.Application.Abstractions;

public sealed record ScreeningAuditEvent(
    Guid Id,
    Guid SubjectId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IScreeningAuditStore
{
    Task AppendAsync(
        ScreeningAuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ScreeningAuditEvent>> GetBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default);
}