using Support.Domain;

namespace Support.Application;

public sealed class EscalationService
{
    public SupportEscalation Escalate(SupportCase supportCase, string level, string reason, DateTimeOffset nowUtc)
    {
        var escalation = new SupportEscalation
        {
            CaseId = supportCase.CaseId,
            Level = level,
            Reason = reason,
            EscalatedAtUtc = nowUtc
        };

        supportCase.Escalations.Add(escalation);
        supportCase.Status = SupportCaseStatus.Escalated;
        return escalation;
    }
}
