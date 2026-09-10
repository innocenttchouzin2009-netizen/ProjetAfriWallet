using AfriWallet.Ledger.Domain;

namespace AfriWallet.Transfer.Application;

public sealed record TransferWalletSnapshot(
    Guid WalletId,
    AccountId AccountId,
    string CurrencyCode,
    bool IsActive);

public interface ITransferWalletReader
{
    Task<TransferWalletSnapshot?> GetAsync(Guid walletId, CancellationToken cancellationToken = default);
}

public interface ITransferBalanceReader
{
    Task<long> GetAvailableMinorAsync(AccountId accountId, string currencyCode, CancellationToken cancellationToken = default);
}

public interface ITransferLedgerPort
{
    Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    Task PostAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default);
}
