namespace Reporting.Domain.Metrics;

public sealed record BusinessMetric(
    string MetricCode,
    string DisplayName,
    decimal Value,
    string Unit,
    DateTime CalculatedAtUtc,
    IReadOnlyDictionary<string, string>? Dimensions = null);
