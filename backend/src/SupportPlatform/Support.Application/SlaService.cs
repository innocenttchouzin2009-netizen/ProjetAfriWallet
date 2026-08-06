using Support.Domain;

namespace Support.Application;

public sealed class SlaService
{
    private readonly Dictionary<SupportCasePriority, (TimeSpan FirstResponse, TimeSpan Resolution)> _targets;

    public SlaService(Dictionary<SupportCasePriority, (TimeSpan FirstResponse, TimeSpan Resolution)>? targets = null)
    {
        _targets = targets ?? new Dictionary<SupportCasePriority, (TimeSpan FirstResponse, TimeSpan Resolution)>
        {
            [SupportCasePriority.Low] = (TimeSpan.FromHours(24), TimeSpan.FromDays(5)),
            [SupportCasePriority.Normal] = (TimeSpan.FromHours(8), TimeSpan.FromDays(3)),
            [SupportCasePriority.High] = (TimeSpan.FromHours(2), TimeSpan.FromHours(24)),
            [SupportCasePriority.Urgent] = (TimeSpan.FromMinutes(30), TimeSpan.FromHours(4)),
            [SupportCasePriority.Critical] = (TimeSpan.FromMinutes(10), TimeSpan.FromHours(1))
        };
    }

    public SupportSla BuildSla(string policyId, SupportCasePriority priority)
    {
        var target = _targets[priority];
        var warningLead = priority switch
        {
            SupportCasePriority.Low => TimeSpan.FromHours(2),
            SupportCasePriority.Normal => TimeSpan.FromHours(1),
            SupportCasePriority.High => TimeSpan.FromMinutes(30),
            SupportCasePriority.Urgent => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(5)
        };

        return new SupportSla
        {
            PolicyId = policyId,
            FirstResponseTarget = target.FirstResponse,
            ResolutionTarget = target.Resolution,
            WarningLeadTime = warningLead
        };
    }

    public (bool WarningTriggered, bool Breached, List<string> NewViolations) Evaluate(SupportCase supportCase, DateTimeOffset nowUtc)
    {
        var sla = supportCase.Sla;
        var elapsed = nowUtc - supportCase.OpenedAtUtc - sla.TotalPausedDuration;
        if (sla.IsPaused && sla.PausedAtUtc.HasValue)
        {
            elapsed -= nowUtc - sla.PausedAtUtc.Value;
        }

        var warningTriggered = elapsed >= sla.ResolutionTarget - sla.WarningLeadTime;
        var breached = false;
        var newViolations = new List<string>();

        if (!sla.FirstResponseAtUtc.HasValue && elapsed > sla.FirstResponseTarget && sla.FirstResponseBreachedAtUtc is null)
        {
            sla.FirstResponseBreachedAtUtc = nowUtc;
            var violation = "FIRST_RESPONSE_BREACHED";
            sla.Violations.Add(violation);
            newViolations.Add(violation);
            breached = true;
        }

        if (!sla.ResolvedAtUtc.HasValue && elapsed > sla.ResolutionTarget && sla.ResolutionBreachedAtUtc is null)
        {
            sla.ResolutionBreachedAtUtc = nowUtc;
            var violation = "RESOLUTION_BREACHED";
            sla.Violations.Add(violation);
            newViolations.Add(violation);
            breached = true;
        }

        return (warningTriggered, breached, newViolations);
    }

    public void Pause(SupportCase supportCase, DateTimeOffset nowUtc)
    {
        if (supportCase.Sla.IsPaused)
        {
            return;
        }

        supportCase.Sla.IsPaused = true;
        supportCase.Sla.PausedAtUtc = nowUtc;
    }

    public void Resume(SupportCase supportCase, DateTimeOffset nowUtc)
    {
        if (!supportCase.Sla.IsPaused || !supportCase.Sla.PausedAtUtc.HasValue)
        {
            return;
        }

        supportCase.Sla.TotalPausedDuration += nowUtc - supportCase.Sla.PausedAtUtc.Value;
        supportCase.Sla.PausedAtUtc = null;
        supportCase.Sla.IsPaused = false;
    }
}
