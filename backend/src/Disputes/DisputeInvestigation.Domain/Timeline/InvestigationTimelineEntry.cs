namespace AfriWallet.Disputes.Investigation.Domain.Timeline;

public sealed record InvestigationTimelineEntry(
    Guid EntryId,
    string EventType,
    string Actor,
    string Description,
    DateTimeOffset OccurredAtUtc);
