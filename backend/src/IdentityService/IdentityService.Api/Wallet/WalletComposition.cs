using AfriWallet.Transfer.Infrastructure;
using AfriWallet.Wallet.Application;

namespace IdentityService.Api.Wallet;

public static class WalletComposition
{
    public static IServiceCollection AddWalletOverview(this IServiceCollection services)
    {
        services.AddScoped<IMobileWalletBalanceReader, LedgerBackedMobileWalletBalanceReader>();
        services.AddScoped<MobileWalletReadApplicationService>();
        return services;
    }
}
