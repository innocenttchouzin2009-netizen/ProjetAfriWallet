using Microsoft.Extensions.Options;

namespace MobileMoney.Production.Telemetry;

public sealed class MobileMoneyTelemetryOptionsValidator : IValidateOptions<MobileMoneyTelemetryOptions>
{
    public ValidateOptionsResult Validate(string? name, MobileMoneyTelemetryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            return ValidateOptionsResult.Fail("ServiceName is required.");
        }

        if (options.EnableOtlpExporter && string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            return ValidateOptionsResult.Fail("OTLP endpoint is required when OTLP exporter is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
