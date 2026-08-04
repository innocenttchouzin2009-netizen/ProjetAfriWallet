using Microsoft.Extensions.Options;
using MobileMoney.Production.Telemetry;

namespace MobileMoney.Production.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddMobileMoneyOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MobileMoneyTelemetryOptions>()
            .Bind(configuration.GetSection(MobileMoneyTelemetryOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MobileMoneyTelemetryOptions>, MobileMoneyTelemetryOptionsValidator>();
        services.AddSingleton<MobileMoneyTelemetry>();
        services.AddSingleton<MobileMoneyTelemetryEnricher>();

        return services;
    }
}
