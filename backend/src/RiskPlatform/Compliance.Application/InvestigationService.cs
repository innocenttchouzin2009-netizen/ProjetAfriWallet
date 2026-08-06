using Compliance.Domain;

namespace Compliance.Application;

public sealed class InvestigationService
{
    public Investigation CreateInvestigation(string summary, string outcome)
        => new() { Summary = summary, Outcome = outcome };

    public void AddInvestigation(ComplianceCase entity, Investigation investigation)
    {
        entity.Investigations.Add(investigation);
        AddNote(entity, "INVESTIGATOR", investigation.Summary);
    }

    public void AddNote(ComplianceCase entity, string author, string message)
    {
        entity.Notes.Add(new InvestigatorNote
        {
            Author = author,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
