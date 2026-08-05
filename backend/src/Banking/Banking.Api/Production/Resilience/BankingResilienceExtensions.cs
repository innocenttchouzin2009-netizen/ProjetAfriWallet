namespace AfriWallet.Banking.Api.Production.Resilience;

public static class BankingResilienceExtensions
{
    public static IServiceCollection AddBankingResilience(this IServiceCollection services)
    {
        services.AddSingleton<BankingResiliencePolicy>();
        return services;
    }
}
