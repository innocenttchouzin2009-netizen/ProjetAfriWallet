using Compliance.Domain;

namespace Compliance.Application;

public sealed class EvidenceService
{
    public Evidence AddEvidence(ComplianceCase entity, string label, string content)
    {
        var evidence = new Evidence
        {
            Label = label,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
        entity.Evidence.Add(evidence);
        return evidence;
    }
}
