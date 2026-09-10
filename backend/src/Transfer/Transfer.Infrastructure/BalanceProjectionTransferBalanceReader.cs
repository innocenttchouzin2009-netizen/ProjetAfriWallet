using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class BalanceProjectionTransferBalanceReader(
    LedgerBackedBalanceReadService balanceReadService,
    ITransferFundsAvailabilityPolicy availabilityPolicy) : ITransferBalanceReader
{
    public async Task<long> GetAvailableMinorAsync(
        AccountId accountId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await balanceReadService.ReadAsync(
            new BalanceKey(accountId, currencyCode),
            cancellationToken);

        return availabilityPolicy.GetAvailableMinor(
            new TransferBalanceProjection(snapshot.DebitMinor, snapshot.CreditMinor, snapshot.NetMinor));
    }
}
