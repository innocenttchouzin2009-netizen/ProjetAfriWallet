using AfriWallet.Ledger.Domain;
using AfriWallet.Reconciliation.Application;
using AfriWallet.Reconciliation.Infrastructure;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;

namespace IdentityService.Api.Reconciliation;

public static class ReconciliationComposition
{
    public static IServiceCollection AddReconciliationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mappings = LoadWalletLedgerAccountMappings(configuration);

        services.AddScoped<TransferReceiptLookupService>();
        services.AddScoped<ITransferAccountWalletProjection>(serviceProvider =>
            new TransferAccountWalletProjection(
                mappings,
                serviceProvider.GetRequiredService<ITransferWalletReader>(),
                serviceProvider.GetRequiredService<IWalletRepository>()));
        services.AddScoped<WalletAwareTransferReconciliationService>();
        return services;
    }

    private static IReadOnlyDictionary<Guid, AccountId> LoadWalletLedgerAccountMappings(IConfiguration configuration)
    {
        var mappings = new Dictionary<Guid, AccountId>();
        foreach (var child in configuration.GetSection("Transfer:WalletLedgerAccounts").GetChildren())
        {
            if (!Guid.TryParse(child.Key, out var walletId) ||
                !Guid.TryParse(child.Value, out var accountGuid))
            {
                throw new InvalidOperationException(
                    "Transfer wallet-ledger account mappings must use GUID wallet keys and GUID account values.");
            }

            mappings[walletId] = new AccountId(accountGuid);
        }

        return mappings;
    }
}
