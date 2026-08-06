using RegulatoryReporting.Domain;

namespace RegulatoryReporting.Application;

public sealed class RegulatoryReportValidator
{
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "compliance_manager",
        "risk_manager",
        "regulatory_officer",
        "admin"
    };

    public void EnsureTransition(RegulatoryReportStatus current, RegulatoryReportStatus next)
    {
        var valid = current switch
        {
            RegulatoryReportStatus.Draft => next == RegulatoryReportStatus.Generated,
            RegulatoryReportStatus.Generated => next == RegulatoryReportStatus.UnderReview,
            RegulatoryReportStatus.UnderReview => next == RegulatoryReportStatus.Approved,
            RegulatoryReportStatus.Approved => next == RegulatoryReportStatus.Submitted,
            RegulatoryReportStatus.Submitted => next == RegulatoryReportStatus.Accepted || next == RegulatoryReportStatus.Rejected,
            RegulatoryReportStatus.Accepted => next == RegulatoryReportStatus.Archived,
            RegulatoryReportStatus.Rejected => next == RegulatoryReportStatus.Generated,
            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException($"Invalid transition from {current} to {next}.");
        }
    }

    public void EnsurePrivilegedRole(string role, string action)
    {
        if (!PrivilegedRoles.Contains(role))
        {
            throw new UnauthorizedAccessException($"Role '{role}' is not allowed to perform '{action}'.");
        }
    }
}
