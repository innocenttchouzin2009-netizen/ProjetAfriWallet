namespace Reporting.Contracts.Responses;

public sealed record ExecutiveDashboardResponse(
    DateTime GeneratedAtUtc,
    IReadOnlyCollection<MetricResponse> Metrics,
    IReadOnlyCollection<AlertResponse> Alerts);

public sealed record MetricResponse(
    string Code,
    string DisplayName,
    decimal Value,
    string Unit);

public sealed record AlertResponse(
    string Code,
    string Severity,
    string Message,
    DateTime OccurredAtUtc);
