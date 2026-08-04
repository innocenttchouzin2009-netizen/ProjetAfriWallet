using Microsoft.Extensions.Options;
using MobileMoney.Production.FeatureFlags;

namespace MobileMoney.Production.Extensions;

public static class FeatureFlagExtensions
{
    public static IServiceCollection AddMobileMoneyFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FeatureFlagOptions>()
            .Bind(configuration.GetSection(FeatureFlagOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<FeatureFlagOptions>, FeatureFlagOptionsValidator>();
        services.AddSingleton<IMobileMoneyFeatureManager, MobileMoneyFeatureManager>();
        services.AddSingleton<FeatureFlagDiagnosticService>();
        return services;
    }
}
