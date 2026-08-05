namespace AfriWallet.Banking.Api.Production.Telemetry;

public static class BankingTelemetryExtensions
{
    public static IServiceCollection AddBankingTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<BankingTelemetryService>();
        return services;
    }
}
