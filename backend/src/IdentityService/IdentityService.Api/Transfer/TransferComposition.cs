using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Infrastructure;

namespace IdentityService.Api.Transfer;

public static class TransferComposition
{
    public static IServiceCollection AddInternalTransferModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mappings = LoadWalletLedgerAccountMappings(configuration);

        services.AddSingleton<IWalletLedgerAccountResolver>(
            new ConfiguredWalletLedgerAccountResolver(mappings));
        services.AddSingleton<ITransferFundsAvailabilityPolicy, NonNegativeNetTransferFundsAvailabilityPolicy>();
        services.AddScoped<ITransferWalletReader, WalletRegistryTransferWalletReader>();
        services.AddScoped<ITransferBalanceReader, BalanceProjectionTransferBalanceReader>();
        services.AddScoped<ITransferLedgerPort, UniversalLedgerTransferLedgerPort>();
        services.AddScoped<InternalTransferPlanningService>();
        services.AddScoped<InternalTransferOrchestrationService>();

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
