using Reporting.Domain.Metrics;

namespace Reporting.Domain.Dashboards;

public sealed record DashboardSnapshot(
    DateTime GeneratedAtUtc,
    IReadOnlyCollection<BusinessMetric> Metrics,
    IReadOnlyCollection<DashboardAlert> Alerts);

public sealed record DashboardAlert(
    string Code,
    string Severity,
    string Message,
    DateTime OccurredAtUtc);
