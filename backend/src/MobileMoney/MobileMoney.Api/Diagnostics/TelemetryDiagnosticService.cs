namespace MobileMoney.Production.Diagnostics;

public sealed class TelemetryDiagnosticService
{
    private readonly MobileMoney.Production.Telemetry.MobileMoneyTelemetryOptions _options;

    public TelemetryDiagnosticService(MobileMoney.Production.Telemetry.MobileMoneyTelemetryOptions options)
    {
        _options = options;
    }

    public object GetDiagnosticSnapshot()
    {
        return new
        {
            exporters = new
            {
                console = _options.EnableConsoleExporter,
                otlp = _options.EnableOtlpExporter,
                prometheus = _options.EnablePrometheusExporter
            },
            serviceName = _options.ServiceName,
            serviceVersion = _options.ServiceVersion,
            environment = _options.Environment,
            activitySources = new[]
            {
                "mobile-money.mtn-momo.deposit",
                "mobile-money.mtn-momo.withdrawal",
                "mobile-money.mtn-momo.status",
                "mobile-money.mtn-momo.callback",
                "mobile-money.mtn-momo.token",
                "mobile-money.mtn-momo.provider-call"
            },
            meters = new[]
            {
                "afw_mobile_money_requests_total",
                "afw_mobile_money_success_total",
                "afw_mobile_money_failure_total",
                "afw_mobile_money_retries_total",
                "afw_mobile_money_timeouts_total",
                "afw_mobile_money_rate_limited_total",
                "afw_mobile_money_callbacks_total",
                "afw_mobile_money_request_duration_ms",
                "afw_mobile_money_provider_duration_ms",
                "afw_mobile_money_transaction_completion_ms"
            }
        };
    }
}
