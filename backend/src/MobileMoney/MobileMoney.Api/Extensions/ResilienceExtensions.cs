using Microsoft.Extensions.Options;
using MobileMoney.Production.Configuration;
using MobileMoney.Production.Logging;
using MobileMoney.Production.Resilience;

namespace MobileMoney.Production.Extensions;

public static class ResilienceExtensions
{
    public static IServiceCollection AddMtnMomoResilience(this IServiceCollection services)
    {
        services.AddOptions<ResilienceOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection(ResilienceOptions.SectionName).Bind(options);
            })
            .ValidateOnStart();

        services.AddSingleton<ResiliencePipelineFactory>();
        services.AddSingleton<ProviderPipelineRegistry>();
        services.AddSingleton<StructuredOperationLogger>();
        return services;
    }
}
