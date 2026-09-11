using AfriWallet.P2P.Application;
using AfriWallet.P2P.Infrastructure;

namespace IdentityService.Api.P2P;

public static class P2PComposition
{
    public static IServiceCollection AddP2PCore(this IServiceCollection services)
    {
        services.AddScoped<WalletRecipientSelector>();
        services.AddScoped<IAfWalIdRecipientLookup, AfWalIdRecipientLookup>();
        services.AddScoped<IQrRecipientLookup, QrRecipientLookup>();
        services.AddScoped<RecipientResolutionService>();
        services.AddScoped<IP2PTransferPort, InternalTransferP2PPort>();
        services.AddScoped<P2PTransferOrchestrationService>();
        return services;
    }

    public static IServiceCollection AddP2PRecipientDirectoryProviders(
        this IServiceCollection services,
        IAfWalIdentityDirectory afWalIdentityDirectory,
        IQrRecipientDirectory qrRecipientDirectory)
    {
        ArgumentNullException.ThrowIfNull(afWalIdentityDirectory);
        ArgumentNullException.ThrowIfNull(qrRecipientDirectory);

        services.AddSingleton(afWalIdentityDirectory);
        services.AddSingleton(qrRecipientDirectory);
        return services;
    }
}
