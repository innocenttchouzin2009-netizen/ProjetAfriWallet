namespace MobileMoney.Production.Telemetry;

public sealed class MobileMoneyTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = "afriwallet-mobile-money";
    public string ServiceVersion { get; init; } = "0.7.3.4";
    public string Environment { get; init; } = "Development";
    public bool EnableConsoleExporter { get; init; }
    public bool EnableOtlpExporter { get; init; }
    public string OtlpEndpoint { get; init; } = "http://localhost:4317";
    public bool EnablePrometheusExporter { get; init; }
}
