namespace MobileMoney.Production.Authentication;

public static class MobileMoneyAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAfWalSessionValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options =
            AfWalSessionValidationOptions.FromConfiguration(configuration);

        services.AddSingleton(options);
        services.AddHttpClient<IAfWalSessionValidator, HttpAfWalSessionValidator>(
            client => client.Timeout = options.Timeout);

        return services;
    }
}
