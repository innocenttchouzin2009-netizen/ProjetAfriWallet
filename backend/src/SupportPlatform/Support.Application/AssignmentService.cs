using Support.Domain;

namespace Support.Application;

public sealed class AssignmentService
{
    private readonly Dictionary<SupportCaseCategory, string> _autoAssignment = new()
    {
        [SupportCaseCategory.Account] = "SUPPORT_ACCOUNT",
        [SupportCaseCategory.Wallet] = "SUPPORT_WALLET",
        [SupportCaseCategory.Payment] = "SUPPORT_PAYMENTS",
        [SupportCaseCategory.MobileMoney] = "SUPPORT_MOBILE_MONEY",
        [SupportCaseCategory.Banking] = "SUPPORT_BANKING",
        [SupportCaseCategory.Card] = "SUPPORT_CARDS",
        [SupportCaseCategory.Merchant] = "SUPPORT_MERCHANT",
        [SupportCaseCategory.DeveloperApi] = "SUPPORT_DEVELOPER"
    };

    public SupportAssignment AssignAutomatically(SupportCase supportCase, DateTimeOffset nowUtc)
    {
        var team = _autoAssignment.TryGetValue(supportCase.Category, out var resolvedTeam)
            ? resolvedTeam
            : "SUPPORT_GENERAL";

        var assignment = new SupportAssignment
        {
            CaseId = supportCase.CaseId,
            Team = team,
            IsAutomatic = true,
            AssignedAtUtc = nowUtc
        };

        supportCase.AssignedTeam = team;
        supportCase.AssignedAgentId = null;
        supportCase.Assignments.Add(assignment);
        return assignment;
    }

    public SupportAssignment AssignManually(SupportCase supportCase, string team, string? agentId, DateTimeOffset nowUtc)
    {
        var assignment = new SupportAssignment
        {
            CaseId = supportCase.CaseId,
            Team = team,
            AgentId = agentId,
            IsAutomatic = false,
            AssignedAtUtc = nowUtc
        };

        supportCase.AssignedTeam = team;
        supportCase.AssignedAgentId = agentId;
        supportCase.Assignments.Add(assignment);
        return assignment;
    }
}
