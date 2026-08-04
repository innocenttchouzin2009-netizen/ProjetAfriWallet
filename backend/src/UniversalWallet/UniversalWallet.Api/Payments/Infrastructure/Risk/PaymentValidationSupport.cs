using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Domain.Balance;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Infrastructure.Risk;

public sealed record PaymentProjectionHarness(
    BalanceProjectionService Service,
    InMemoryBalanceProjectionRepository ProjectionRepository,
    InMemoryLedgerRepository LedgerRepository);

public static class PaymentValidationSupport
{
    public static PaymentProjectionHarness CreateBalanceProjectionService(IWalletRepository walletRepository)
    {
        var ledgerRepository = new InMemoryLedgerRepository();
        var projectionRepository = new InMemoryBalanceProjectionRepository();
        var service = new BalanceProjectionService(
            ledgerRepository,
            new WalletCurrencyReader(walletRepository),
            projectionRepository,
            new InMemoryBalanceSnapshotRepository(),
            new InMemoryProjectionVersionRepository());

        return new PaymentProjectionHarness(service, projectionRepository, ledgerRepository);
    }

    public static void SeedProjection(PaymentProjectionHarness harness, Wallet wallet)
    {
        harness.ProjectionRepository.Upsert(new WalletBalanceProjection(
            wallet.Id,
            wallet.Currency,
            0m,
            wallet.AvailableBalance,
            wallet.PendingBalance,
            wallet.ReservedBalance,
            0m,
            0m,
            0,
            DateTimeOffset.UtcNow));
    }
}
