using Support.Domain;

namespace Support.Infrastructure;

public sealed class InMemorySupportStore
{
    public List<SupportCase> Cases { get; } = new();
    public Dictionary<string, long> Metrics { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["afw_support_cases_created_total"] = 0,
        ["afw_support_cases_open_total"] = 0,
        ["afw_support_cases_resolved_total"] = 0,
        ["afw_support_cases_escalated_total"] = 0,
        ["afw_support_sla_breaches_total"] = 0,
        ["afw_support_first_response_duration_ms"] = 0,
        ["afw_support_resolution_duration_ms"] = 0
    };
}
