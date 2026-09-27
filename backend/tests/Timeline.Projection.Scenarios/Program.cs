using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;
using AfriWallet.Timeline.Application;
using AfriWallet.Timeline.Domain;
using AfriWallet.Timeline.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

await ProjectionMapsDebitAndCreditDirectionsAsync();
await ReaderScopesByOwnerAndWalletAndOrdersNewestFirstAsync();
await ReaderRejectsMissingLedgerMappingAsync();

Console.WriteLine("Timeline projection scenarios passed.");

static Task ProjectionMapsDebitAndCreditDirectionsAsync()
{
    var service = new FinancialActivityProjectionService();
    var ownerId = Guid.NewGuid();
    var walletId = WalletId.New();
    var walletAccount = AccountId.New();
    var counterparty = AccountId.New();
    var timestamp = new DateTimeOffset(2026, 9, 27, 8, 0, 0, TimeSpan.Zero);

    var outgoingJournal = JournalEntry.Create(
        JournalEntryId.New(),
        "XAF",
        "TRF-OUTGOING",
        Guid.NewGuid(),
        timestamp,
        [
            new LedgerLine(walletAccount, LedgerSide.Debit, 5_000),
            new LedgerLine(counterparty, LedgerSide.Credit, 5_000)
        ]);

    var incomingJournal = JournalEntry.Create(
        JournalEntryId.New(),
        "XAF",
        "CASH-IN",
        Guid.NewGuid(),
        timestamp.AddMinutes(1),
        [
            new LedgerLine(counterparty, LedgerSide.Debit, 7_500),
            new LedgerLine(walletAccount, LedgerSide.Credit, 7_500)
        ]);

    var outgoing = service.Project(ownerId, walletId, walletAccount, outgoingJournal);
    var incoming = service.Project(ownerId, walletId, walletAccount, incomingJournal);

    Assert(outgoing is not null, "Outgoing activity must be projected.");
    Assert(outgoing!.Direction == FinancialActivityDirection.Outgoing, "Debit must project as outgoing.");
    Assert(outgoing.AmountMinor == 5_000, "Outgoing amount mismatch.");
    Assert(outgoing.Kind == FinancialActivityKind.LedgerPosting, "Ledger projection must remain generic.");
    Assert(outgoing.State == FinancialActivityState.Completed, "Posted ledger activity must be completed.");
    Assert(outgoing.Source.System == "ledger", "Ledger source system mismatch.");
    Assert(outgoing.Summary == "TRF-OUTGOING", "Business reference must be preserved.");

    Assert(incoming is not null, "Incoming activity must be projected.");
    Assert(incoming!.Direction == FinancialActivityDirection.Incoming, "Credit must project as incoming.");
    Assert(incoming.AmountMinor == 7_500, "Incoming amount mismatch.");

    return Task.CompletedTask;
}

static async Task ReaderScopesByOwnerAndWalletAndOrdersNewestFirstAsync()
{
    var ownerId = Guid.NewGuid();
    var walletOne = Wallet.Create(
        WalletId.New(),
        ownerId,
        Currency.Create("XAF"),
        null,
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
    var walletTwo = Wallet.Create(
        WalletId.New(),
        ownerId,
        Currency.Create("XAF"),
        null,
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));

    var accountOne = AccountId.New();
    var accountTwo = AccountId.New();
    var counterparty = AccountId.New();

    var firstJournal = JournalEntry.Create(
        JournalEntryId.New(),
        "XAF",
        "OLDER",
        Guid.NewGuid(),
        new DateTimeOffset(2026, 9, 27, 8, 0, 0, TimeSpan.Zero),
        [
            new LedgerLine(accountOne, LedgerSide.Debit, 1_000),
            new LedgerLine(counterparty, LedgerSide.Credit, 1_000)
        ]);
    var secondJournal = JournalEntry.Create(
        JournalEntryId.New(),
        "XAF",
        "NEWER",
        Guid.NewGuid(),
        new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero),
        [
            new LedgerLine(counterparty, LedgerSide.Debit, 2_000),
            new LedgerLine(accountTwo, LedgerSide.Credit, 2_000)
        ]);

    var repository = new FixedWalletRepository([walletOne, walletTwo]);
    var resolver = new FixedAccountResolver(new Dictionary<WalletId, AccountId>
    {
        [walletOne.Id] = accountOne,
        [walletTwo.Id] = accountTwo
    });
    var journalReader = new FixedJournalReader(new Dictionary<(Guid AccountId, string Currency), IReadOnlyList<JournalEntry>>
    {
        [(accountOne.Value, "XAF")] = [firstJournal],
        [(accountTwo.Value, "XAF")] = [secondJournal]
    });

    var reader = new LedgerBackedFinancialActivityReader(
        repository,
        resolver,
        journalReader,
        new FinancialActivityProjectionService());

    var all = await reader.ReadAsync(FinancialActivityQuery.Create(ownerId));

    Assert(all.Items.Count == 2, "Owner timeline must include both wallets.");
    Assert(all.Items[0].Summary == "NEWER", "Timeline must be ordered newest first.");
    Assert(all.Items[1].Summary == "OLDER", "Older activity order mismatch.");

    var scoped = await reader.ReadAsync(FinancialActivityQuery.Create(ownerId, walletOne.Id));

    Assert(scoped.Items.Count == 1, "Wallet-scoped timeline must include only the selected wallet.");
    Assert(scoped.Items[0].WalletId == walletOne.Id, "Wallet scope mismatch.");

    var foreignWallet = await reader.ReadAsync(FinancialActivityQuery.Create(ownerId, WalletId.New()));
    Assert(foreignWallet.Items.Count == 0, "Unknown wallet in owner scope must not leak activity.");
}

static async Task ReaderRejectsMissingLedgerMappingAsync()
{
    var ownerId = Guid.NewGuid();
    var wallet = Wallet.Create(
        WalletId.New(),
        ownerId,
        Currency.Create("EUR"),
        null,
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));

    var reader = new LedgerBackedFinancialActivityReader(
        new FixedWalletRepository([wallet]),
        new FixedAccountResolver(new Dictionary<WalletId, AccountId>()),
        new FixedJournalReader(new Dictionary<(Guid AccountId, string Currency), IReadOnlyList<JournalEntry>>()),
        new FinancialActivityProjectionService());

    await ExpectAsync<InvalidOperationException>(
        () => reader.ReadAsync(FinancialActivityQuery.Create(ownerId)));
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task ExpectAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

sealed class FixedWalletRepository(IReadOnlyList<Wallet> wallets) : IWalletRepository
{
    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(wallets.Any(wallet =>
            wallet.OwnerId == ownerId &&
            string.Equals(wallet.Currency.Code, currencyCode.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(wallets.SingleOrDefault(wallet => wallet.Id == walletId));

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(wallets.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

sealed class FixedAccountResolver(IReadOnlyDictionary<WalletId, AccountId> accounts)
    : IFinancialActivityWalletAccountResolver
{
    public Task<AccountId?> ResolveAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(accounts.TryGetValue(walletId, out var accountId)
            ? (AccountId?)accountId
            : null);
}

sealed class FixedJournalReader(
    IReadOnlyDictionary<(Guid AccountId, string Currency), IReadOnlyList<JournalEntry>> journals)
    : ILedgerJournalReader
{
    public Task<IReadOnlyList<JournalEntry>> ReadAsync(
        BalanceKey key,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(journals.TryGetValue((key.AccountId.Value, key.CurrencyCode), out var entries)
            ? entries
            : (IReadOnlyList<JournalEntry>)Array.Empty<JournalEntry>());
}
