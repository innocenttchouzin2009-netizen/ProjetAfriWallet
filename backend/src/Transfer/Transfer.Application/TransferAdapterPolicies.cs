using AfriWallet.Ledger.Domain;

namespace AfriWallet.Transfer.Application;

public sealed record TransferBalanceProjection(long DebitMinor, long CreditMinor, long NetMinor);

public interface IWalletLedgerAccountResolver
{
    Task<AccountId?> ResolveAsync(Guid walletId, CancellationToken cancellationToken = default);
}

public interface ITransferFundsAvailabilityPolicy
{
    long GetAvailableMinor(TransferBalanceProjection projection);
}
