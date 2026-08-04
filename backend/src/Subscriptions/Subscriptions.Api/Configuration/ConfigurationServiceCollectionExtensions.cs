using Microsoft.Extensions.Options;

namespace Subscriptions.Api.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddEnterpriseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MtnMomoOptions>()
            .Bind(configuration.GetSection(MtnMomoOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MtnMomoOptions>, MtnMomoOptionsValidation>();
        services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();

        return services;
    }
}
