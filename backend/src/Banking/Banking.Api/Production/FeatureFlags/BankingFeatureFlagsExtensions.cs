namespace AfriWallet.Banking.Api.Production.FeatureFlags;

public static class BankingFeatureFlagsExtensions
{
    public static IServiceCollection AddBankingFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new BankingFeatureFlags
        {
            BankingEnabled = configuration.GetValue("Banking:Enabled", false),
            SepaEnabled = configuration.GetValue("SEPA:Enabled", false),
            SwiftEnabled = configuration.GetValue("SWIFT:Enabled", false),
            DomesticEnabled = configuration.GetValue("Domestic:Enabled", false),
            SandboxEnabled = configuration.GetValue("Sandbox:Enabled", true),
            ProductionEnabled = configuration.GetValue("Production:Enabled", false),
            TimelineEnabled = configuration.GetValue("Timeline:Enabled", true),
            NotificationsEnabled = configuration.GetValue("Notifications:Enabled", true)
        });

        return services;
    }
}
