using Compliance.Domain;

namespace Compliance.Application;

public sealed class EscalationService
{
    public void Escalate(ComplianceCase entity, string reason, string escalatedBy)
    {
        entity.Status = CaseStatus.Escalated;
        entity.Notes.Add(new InvestigatorNote
        {
            Author = escalatedBy,
            Message = $"Escalation reason: {reason}",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
