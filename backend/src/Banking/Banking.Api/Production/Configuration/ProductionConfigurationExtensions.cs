using Microsoft.Extensions.Options;

namespace AfriWallet.Banking.Api.Production.Configuration;

public static class ProductionConfigurationExtensions
{
    public static IServiceCollection AddBankingProductionConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BankingProductionOptions>()
            .Bind(configuration.GetSection(BankingProductionOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<BankingProductionOptions>, BankingProductionOptionsValidator>();

        services.AddSingleton<BankingProductionConfigurationService>();
        services.AddSingleton<BankingProductionSecretValidator>();

        return services;
    }
}
