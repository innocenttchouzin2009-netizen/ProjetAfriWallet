using RegulatoryReporting.Domain;

namespace RegulatoryReporting.Application;

public sealed class RegulatoryReportAuditService
{
    public void Record(RegulatoryReport report, string @event, string actor)
    {
        var line = $"{DateTimeOffset.UtcNow:O}|{actor}|{@event}";
        report.AuditEvents.Add(@event);
        report.AuditTimeline.Add(line);
    }
}
