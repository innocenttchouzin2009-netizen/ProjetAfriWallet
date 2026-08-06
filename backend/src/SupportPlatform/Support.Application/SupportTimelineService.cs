using Support.Domain;

namespace Support.Application;

public sealed class SupportTimelineService
{
    public void Add(SupportCase supportCase, string eventType, string actorId, string description, DateTimeOffset nowUtc)
    {
        supportCase.Timeline.Add(new SupportTimelineEntry
        {
            CaseId = supportCase.CaseId,
            EventType = eventType,
            ActorId = actorId,
            Description = description,
            CreatedAtUtc = nowUtc
        });
    }
}
