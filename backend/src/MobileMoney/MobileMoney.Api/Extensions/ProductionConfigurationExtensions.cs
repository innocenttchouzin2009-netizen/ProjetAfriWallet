using Microsoft.Extensions.Options;
using MobileMoney.Production.Configuration;
using MobileMoney.Production.Diagnostics;
using MobileMoney.Production.Health;
using MobileMoney.Production.Secrets;

namespace MobileMoney.Production.Extensions;

public static class ProductionConfigurationExtensions
{
    public static IServiceCollection AddMtnMomoProductionConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MtnMomoProductionOptions>()
            .Bind(configuration.GetSection(MtnMomoProductionOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MtnMomoProductionOptions>, MtnMomoOptionsValidator>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MtnMomoProductionOptions>>().Value);
        services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();
        services.AddSingleton<CachedSecretProvider>();
        services.AddSingleton<ConfigurationDiagnosticService>();
        services.AddSingleton<ProductionEnvironmentGuard>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MtnMomoProductionOptions>>().Value;
            return new ProductionEnvironmentGuard(options);
        });

        services.AddMtnMomoResilience();

        services.AddSingleton<IHealthProbe, ConfigurationHealthProbe>();
        services.AddSingleton<IHealthProbe, SecretProviderHealthProbe>();
        services.AddSingleton<IHealthProbe, ConnectorHealthProbe>();
        services.AddSingleton<IHealthProbe, ReadinessHealthProbe>();
        services.AddSingleton<HealthProbeRegistry>();

        return services;
    }
}
