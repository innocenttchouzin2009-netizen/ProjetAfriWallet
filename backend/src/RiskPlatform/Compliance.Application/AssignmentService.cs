using Compliance.Domain;

namespace Compliance.Application;

public sealed class AssignmentService
{
    public string AutoAssignInvestigator(string source)
    {
        return source.Trim().ToUpperInvariant() switch
        {
            "FRAUD" => "fraud-queue@afriwallet",
            "AML" => "aml-queue@afriwallet",
            "RISK_SCORING" => "risk-queue@afriwallet",
            "DEVICE" => "device-queue@afriwallet",
            "REGULATORY_RULE" => "regulatory-queue@afriwallet",
            _ => "compliance-queue@afriwallet"
        };
    }

    public void Assign(ComplianceCase entity, string investigator, bool automatic)
    {
        entity.AssignedInvestigator = investigator;
        entity.Status = CaseStatus.UnderReview;
        entity.Notes.Add(new InvestigatorNote
        {
            Author = "SYSTEM",
            Message = automatic ? $"Auto-assigned to {investigator}" : $"Manually assigned to {investigator}",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
