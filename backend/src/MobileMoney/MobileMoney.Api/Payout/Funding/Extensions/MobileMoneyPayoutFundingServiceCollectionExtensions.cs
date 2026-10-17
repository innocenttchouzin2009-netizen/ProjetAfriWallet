using AfriWallet.Balance.Application;
using AfriWallet.Balance.Infrastructure;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Infrastructure;
using MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

namespace MobileMoney.Production.Payout.Funding.Extensions;

public static class MobileMoneyPayoutFundingServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutFundingApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<IMobileMoneyPayoutFundingSourceReader>(
            new ConfiguredMobileMoneyPayoutFundingSourceReader(configuration));

        AddAuthoritativeWalletFunding(services, configuration);

        var connectionString =
            configuration.GetConnectionString("MobileMoneyPayoutFunding")
            ?? "Data Source=mobile-money-payout-funding.db";

        services.AddDbContext<MobileMoneyPayoutFundingDbContext>(dbOptions =>
            dbOptions.UseSqlite(connectionString));

        services.TryAddScoped<
            IMobileMoneyPayoutFundingAttemptStore,
            EfMobileMoneyPayoutFundingAttemptStore>();

        services.TryAddSingleton<MobileMoneyPayoutSplitFundingValidator>();
        services.TryAddScoped<MobileMoneyPayoutFundingPlanningService>();

        return services;
    }

    private static void AddAuthoritativeWalletFunding(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var walletConnectionString =
            configuration.GetConnectionString("WalletDatabase")
            ?? Environment.GetEnvironmentVariable("AFW_WALLET_DB_CONNECTION_STRING")
            ?? "Data Source=wallet-registry.db";

        var ledgerConnectionString =
            configuration.GetConnectionString("LedgerDatabase")
            ?? Environment.GetEnvironmentVariable("AFW_LEDGER_DB_CONNECTION_STRING")
            ?? "Data Source=universal-ledger.db";

        services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlite(walletConnectionString));
        services.AddDbContext<LedgerDbContext>(options =>
            options.UseSqlite(ledgerConnectionString));

        services.TryAddScoped<IWalletRepository, EfWalletRepository>();
        services.TryAddScoped<IJournalRepository, EfJournalRepository>();
        services.TryAddScoped<ILedgerJournalReader, EfLedgerJournalReader>();
        services.TryAddScoped<BalanceProjectionService>();
        services.TryAddScoped<LedgerBackedBalanceReadService>();

        services.TryAddSingleton<IWalletLedgerAccountResolver>(
            new ConfiguredWalletLedgerAccountResolver(
                LoadWalletLedgerAccountMappings(configuration)));
        services.TryAddSingleton<
            ITransferFundsAvailabilityPolicy,
            NonNegativeNetTransferFundsAvailabilityPolicy>();
        services.TryAddScoped<
            IMobileWalletBalanceReader,
            LedgerBackedMobileWalletBalanceReader>();
        services.TryAddScoped<
            IMobileMoneyPayoutAuthoritativeWalletSourceReader,
            AuthoritativeMobileMoneyPayoutWalletSourceReader>();
    }

    private static IReadOnlyDictionary<Guid, AccountId>
        LoadWalletLedgerAccountMappings(IConfiguration configuration)
    {
        var mappings = new Dictionary<Guid, AccountId>();
        foreach (var child in configuration
            .GetSection("Transfer:WalletLedgerAccounts")
            .GetChildren())
        {
            if (!Guid.TryParse(child.Key, out var walletId)
                || !Guid.TryParse(child.Value, out var accountGuid))
            {
                throw new InvalidOperationException(
                    "Transfer wallet-ledger account mappings must use GUID wallet keys and GUID account values.");
            }

            mappings[walletId] = new AccountId(accountGuid);
        }

        return mappings;
    }
}
