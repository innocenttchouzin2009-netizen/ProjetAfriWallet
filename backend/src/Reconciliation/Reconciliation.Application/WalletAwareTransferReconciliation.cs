using AfriWallet.Ledger.Domain;

namespace AfriWallet.Reconciliation.Application;

public enum WalletAwareTransferReconciliationStatus
{
    Reconciled = 1,
    NotFound = 2,
    NotTransferJournal = 3,
    InvalidTransferJournal = 4,
    WalletProjectionMissing = 5,
    Inconsistent = 6
}

public sealed record ReconciliationWalletProjection(
    Guid WalletId,
    Guid OwnerId,
    AccountId AccountId,
    string CurrencyCode,
    bool IsActive);

public interface ITransferAccountWalletProjection
{
    Task<ReconciliationWalletProjection?> ResolveAsync(
        AccountId accountId,
        CancellationToken cancellationToken = default);
}

public sealed record WalletAwareTransferReceipt(
    TransferReceipt Transfer,
    ReconciliationWalletProjection SourceWallet,
    ReconciliationWalletProjection TargetWallet);

public sealed record WalletAwareTransferReconciliationResult(
    WalletAwareTransferReconciliationStatus Status,
    WalletAwareTransferReceipt? Receipt,
    string? Reason)
{
    public static WalletAwareTransferReconciliationResult Reconciled(WalletAwareTransferReceipt receipt) =>
        new(WalletAwareTransferReconciliationStatus.Reconciled, receipt, null);

    public static WalletAwareTransferReconciliationResult FromLookup(TransferReceiptLookupResult lookup) =>
        lookup.Status switch
        {
            TransferReceiptLookupStatus.NotFound => new(WalletAwareTransferReconciliationStatus.NotFound, null, lookup.Reason),
            TransferReceiptLookupStatus.NotTransferJournal => new(WalletAwareTransferReconciliationStatus.NotTransferJournal, null, lookup.Reason),
            TransferReceiptLookupStatus.InvalidTransferJournal => new(WalletAwareTransferReconciliationStatus.InvalidTransferJournal, null, lookup.Reason),
            _ => throw new InvalidOperationException("Found transfer lookup must be reconciled through wallet projection.")
        };

    public static WalletAwareTransferReconciliationResult ProjectionMissing(string reason) =>
        new(WalletAwareTransferReconciliationStatus.WalletProjectionMissing, null, reason);

    public static WalletAwareTransferReconciliationResult Inconsistent(string reason) =>
        new(WalletAwareTransferReconciliationStatus.Inconsistent, null, reason);
}

public sealed class WalletAwareTransferReconciliationService(
    TransferReceiptLookupService receiptLookup,
    ITransferAccountWalletProjection walletProjection)
{
    public async Task<WalletAwareTransferReconciliationResult> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var lookup = await receiptLookup.GetByCorrelationIdAsync(correlationId, cancellationToken);
        return await ReconcileAsync(lookup, cancellationToken);
    }

    public async Task<WalletAwareTransferReconciliationResult> GetByJournalEntryIdAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var lookup = await receiptLookup.GetByJournalEntryIdAsync(journalEntryId, cancellationToken);
        return await ReconcileAsync(lookup, cancellationToken);
    }

    private async Task<WalletAwareTransferReconciliationResult> ReconcileAsync(
        TransferReceiptLookupResult lookup,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (lookup.Status != TransferReceiptLookupStatus.Found || lookup.Receipt is null)
        {
            return WalletAwareTransferReconciliationResult.FromLookup(lookup);
        }

        var transfer = lookup.Receipt;
        if (transfer.AmountMinor <= 0)
        {
            return WalletAwareTransferReconciliationResult.Inconsistent("Transfer amount must be positive.");
        }

        var source = await walletProjection.ResolveAsync(transfer.DebitAccountId, cancellationToken);
        var target = await walletProjection.ResolveAsync(transfer.CreditAccountId, cancellationToken);

        if (source is null)
        {
            return WalletAwareTransferReconciliationResult.ProjectionMissing("Source ledger account is not mapped to a wallet.");
        }

        if (target is null)
        {
            return WalletAwareTransferReconciliationResult.ProjectionMissing("Target ledger account is not mapped to a wallet.");
        }

        if (source.AccountId != transfer.DebitAccountId || target.AccountId != transfer.CreditAccountId)
        {
            return WalletAwareTransferReconciliationResult.Inconsistent("Projected wallet/account mapping does not match transfer ledger accounts.");
        }

        if (source.WalletId == Guid.Empty || target.WalletId == Guid.Empty || source.WalletId == target.WalletId)
        {
            return WalletAwareTransferReconciliationResult.Inconsistent("Transfer must project to two distinct non-empty wallets.");
        }

        if (source.OwnerId == Guid.Empty || target.OwnerId == Guid.Empty)
        {
            return WalletAwareTransferReconciliationResult.Inconsistent("Projected wallet ownership is invalid.");
        }

        if (!string.Equals(source.CurrencyCode, transfer.CurrencyCode, StringComparison.Ordinal) ||
            !string.Equals(target.CurrencyCode, transfer.CurrencyCode, StringComparison.Ordinal))
        {
            return WalletAwareTransferReconciliationResult.Inconsistent("Wallet currency does not match transfer journal currency.");
        }

        return WalletAwareTransferReconciliationResult.Reconciled(
            new WalletAwareTransferReceipt(transfer, source, target));
    }
}
