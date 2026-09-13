using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Reconciliation.Application;
using AfriWallet.Reconciliation.Infrastructure;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var sourceWalletId = Guid.NewGuid();
var targetWalletId = Guid.NewGuid();
var sourceOwnerId = Guid.NewGuid();
var targetOwnerId = Guid.NewGuid();
var debitAccount = new AccountId(Guid.NewGuid());
var creditAccount = new AccountId(Guid.NewGuid());
var transferId = Guid.NewGuid();
var correlationId = Guid.NewGuid();
var now = new DateTimeOffset(2026, 9, 13, 5, 30, 0, TimeSpan.Zero);

var sourceWallet = Wallet.Create(WalletId.From(sourceWalletId), sourceOwnerId, Currency.Create("XAF"), null, now);
var targetWallet = Wallet.Create(WalletId.From(targetWalletId), targetOwnerId, Currency.Create("XAF"), null, now);
var wallets = new InMemoryWalletRepository([sourceWallet, targetWallet]);
var transferWallets = new InMemoryTransferWalletReader([
    new TransferWalletSnapshot(sourceWalletId, debitAccount, "XAF", true),
    new TransferWalletSnapshot(targetWalletId, creditAccount, "XAF", true)
]);
var projection = new TransferAccountWalletProjection(
    new Dictionary<Guid, AccountId>
    {
        [sourceWalletId] = debitAccount,
        [targetWalletId] = creditAccount
    },
    transferWallets,
    wallets);

var journal = JournalEntry.Create(
    JournalEntryId.New(),
    "XAF",
    $"TRF-{transferId:N}",
    correlationId,
    now,
    [
        new LedgerLine(debitAccount, LedgerSide.Debit, 25_000, "Internal transfer debit"),
        new LedgerLine(creditAccount, LedgerSide.Credit, 25_000, "Internal transfer credit")
    ]);
var journalRepository = new InMemoryJournalRepository([journal]);
var service = new WalletAwareTransferReconciliationService(
    new TransferReceiptLookupService(journalRepository),
    projection);

var result = await service.GetByCorrelationIdAsync(correlationId);
Assert(result.Status == WalletAwareTransferReconciliationStatus.Reconciled, "Transfer must reconcile.");
Assert(result.Receipt is not null, "Wallet-aware receipt is required.");
Assert(result.Receipt!.Transfer.TransferId == transferId, "Transfer id mismatch.");
Assert(result.Receipt.SourceWallet.WalletId == sourceWalletId, "Source wallet mismatch.");
Assert(result.Receipt.SourceWallet.OwnerId == sourceOwnerId, "Source owner mismatch.");
Assert(result.Receipt.SourceWallet.AccountId == debitAccount, "Source account mismatch.");
Assert(result.Receipt.TargetWallet.WalletId == targetWalletId, "Target wallet mismatch.");
Assert(result.Receipt.TargetWallet.OwnerId == targetOwnerId, "Target owner mismatch.");
Assert(result.Receipt.TargetWallet.AccountId == creditAccount, "Target account mismatch.");
Assert(result.Receipt.SourceWallet.CurrencyCode == "XAF" && result.Receipt.TargetWallet.CurrencyCode == "XAF", "Wallet currency mismatch.");
Assert(result.Receipt.Transfer.AmountMinor == 25_000, "Amount mismatch.");

var byJournal = await service.GetByJournalEntryIdAsync(journal.Id.Value);
Assert(byJournal.Status == WalletAwareTransferReconciliationStatus.Reconciled, "Journal lookup must reconcile.");

var missingProjection = new TransferAccountWalletProjection(
    new Dictionary<Guid, AccountId> { [sourceWalletId] = debitAccount },
    transferWallets,
    wallets);
var missingService = new WalletAwareTransferReconciliationService(
    new TransferReceiptLookupService(journalRepository),
    missingProjection);
var missing = await missingService.GetByCorrelationIdAsync(correlationId);
Assert(missing.Status == WalletAwareTransferReconciliationStatus.WalletProjectionMissing, "Missing target mapping must fail closed.");

var mismatchedCurrencyReader = new InMemoryTransferWalletReader([
    new TransferWalletSnapshot(sourceWalletId, debitAccount, "EUR", true),
    new TransferWalletSnapshot(targetWalletId, creditAccount, "XAF", true)
]);
var mismatchProjection = new TransferAccountWalletProjection(
    new Dictionary<Guid, AccountId>
    {
        [sourceWalletId] = debitAccount,
        [targetWalletId] = creditAccount
    },
    mismatchedCurrencyReader,
    wallets);
try
{
    await mismatchProjection.ResolveAsync(debitAccount);
    throw new InvalidOperationException("Expected adapter currency mismatch.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("currency", StringComparison.OrdinalIgnoreCase)) { }

try
{
    _ = new TransferAccountWalletProjection(
        new Dictionary<Guid, AccountId>
        {
            [Guid.NewGuid()] = debitAccount,
            [Guid.NewGuid()] = debitAccount
        },
        transferWallets,
        wallets);
    throw new InvalidOperationException("Expected duplicate account mapping rejection.");
}
catch (ArgumentException) { }

var suspendedTarget = Wallet.Restore(
    WalletId.From(targetWalletId),
    targetOwnerId,
    Currency.Create("XAF"),
    null,
    WalletStatus.Suspended,
    now,
    now.AddMinutes(1));
var inactiveWallets = new InMemoryWalletRepository([sourceWallet, suspendedTarget]);
var inactiveReader = new InMemoryTransferWalletReader([
    new TransferWalletSnapshot(sourceWalletId, debitAccount, "XAF", true),
    new TransferWalletSnapshot(targetWalletId, creditAccount, "XAF", false)
]);
var inactiveService = new WalletAwareTransferReconciliationService(
    new TransferReceiptLookupService(journalRepository),
    new TransferAccountWalletProjection(
        new Dictionary<Guid, AccountId>
        {
            [sourceWalletId] = debitAccount,
            [targetWalletId] = creditAccount
        },
        inactiveReader,
        inactiveWallets));
var inactive = await inactiveService.GetByCorrelationIdAsync(correlationId);
Assert(inactive.Status == WalletAwareTransferReconciliationStatus.Reconciled, "Historical transfer must still reconcile when wallet is now inactive.");
Assert(inactive.Receipt?.TargetWallet.IsActive == false, "Current inactive state must be projected, not treated as historical inconsistency.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.GetByCorrelationIdAsync(correlationId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-RECON-1 wallet-aware transfer reconciliation scenarios: PASS");

sealed class InMemoryTransferWalletReader(IEnumerable<TransferWalletSnapshot> seed) : ITransferWalletReader
{
    private readonly Dictionary<Guid, TransferWalletSnapshot> wallets = seed.ToDictionary(x => x.WalletId);

    public Task<TransferWalletSnapshot?> GetAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets.TryGetValue(walletId, out var value);
        return Task.FromResult(value);
    }
}

sealed class InMemoryWalletRepository(IEnumerable<Wallet> seed) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> wallets = seed.ToDictionary(x => x.Id.Value);

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(wallets.Values.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets.TryGetValue(walletId.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(wallets.Values.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}

sealed class InMemoryJournalRepository(IEnumerable<JournalEntry> seed) : IJournalRepository
{
    private readonly Dictionary<Guid, JournalEntry> byId = seed.ToDictionary(x => x.Id.Value);
    private readonly Dictionary<Guid, JournalEntry> byCorrelation = seed.ToDictionary(x => x.CorrelationId);

    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(byCorrelation.ContainsKey(correlationId));

    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        byId[journalEntry.Id.Value] = journalEntry;
        byCorrelation[journalEntry.CorrelationId] = journalEntry;
        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byId.TryGetValue(journalEntryId.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byCorrelation.TryGetValue(correlationId, out var value);
        return Task.FromResult(value);
    }
}
