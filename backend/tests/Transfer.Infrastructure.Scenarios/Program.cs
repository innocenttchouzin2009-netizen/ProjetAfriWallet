using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Transfer.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

await RunAsync("wallet adapter uses registry and explicit ledger account resolver", async () =>
{
    var wallet = Wallet.Create(WalletId.New(), Guid.NewGuid(), Currency.Create("xaf"), null, DateTimeOffset.UtcNow);
    var account = AccountId.New();
    var adapter = new WalletRegistryTransferWalletReader(new FakeWalletRepository(wallet), new FakeAccountResolver(wallet.Id.Value, account));
    var snapshot = await adapter.GetAsync(wallet.Id.Value);
    Assert(snapshot is not null && snapshot.AccountId == account && snapshot.CurrencyCode == "XAF" && snapshot.IsActive, "Wallet adapter mismatch.");
});

await RunAsync("wallet adapter rejects unresolved ledger account", async () =>
{
    var wallet = Wallet.Create(WalletId.New(), Guid.NewGuid(), Currency.Create("EUR"), null, DateTimeOffset.UtcNow);
    var adapter = new WalletRegistryTransferWalletReader(new FakeWalletRepository(wallet), new FakeAccountResolver(Guid.NewGuid(), AccountId.New()));
    await ExpectAsync<InvalidOperationException>(() => adapter.GetAsync(wallet.Id.Value));
});

await RunAsync("balance adapter delegates interpretation to availability policy", async () =>
{
    var account = AccountId.New();
    var other = AccountId.New();
    var journal = JournalEntry.Create(JournalEntryId.New(), "XAF", "SEED", Guid.NewGuid(), DateTimeOffset.UtcNow,
        [new LedgerLine(other, LedgerSide.Debit, 5000), new LedgerLine(account, LedgerSide.Credit, 5000)]);
    var readService = new LedgerBackedBalanceReadService(new FakeJournalReader([journal]), new BalanceProjectionService());
    var policy = new RecordingAvailabilityPolicy();
    var adapter = new BalanceProjectionTransferBalanceReader(readService, policy);
    var available = await adapter.GetAvailableMinorAsync(account, "xaf");
    Assert(available == 5000 && policy.LastProjection?.NetMinor == 5000, "Balance policy delegation mismatch.");
});

await RunAsync("ledger adapter delegates correlation lookup and posting", async () =>
{
    var repository = new FakeJournalRepository();
    var adapter = new UniversalLedgerTransferLedgerPort(repository);
    var correlation = Guid.NewGuid();
    Assert(!await adapter.ExistsByCorrelationIdAsync(correlation), "Correlation should not exist initially.");
    var journal = JournalEntry.Create(JournalEntryId.New(), "EUR", "TRF-TEST", correlation, DateTimeOffset.UtcNow,
        [new LedgerLine(AccountId.New(), LedgerSide.Debit, 100), new LedgerLine(AccountId.New(), LedgerSide.Credit, 100)]);
    await adapter.PostAsync(journal);
    Assert(await adapter.ExistsByCorrelationIdAsync(correlation) && repository.Posted == journal, "Ledger adapter mismatch.");
});

Console.WriteLine("Transfer infrastructure scenarios passed.");

static async Task RunAsync(string name, Func<Task> action) { await action(); Console.WriteLine($"PASS: {name}"); }
static async Task ExpectAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

sealed class FakeAccountResolver(Guid walletId, AccountId accountId) : IWalletLedgerAccountResolver
{
    public Task<AccountId?> ResolveAsync(Guid candidate, CancellationToken cancellationToken = default) => Task.FromResult<AccountId?>(candidate == walletId ? accountId : null);
}

sealed class RecordingAvailabilityPolicy : ITransferFundsAvailabilityPolicy
{
    public TransferBalanceProjection? LastProjection { get; private set; }
    public long GetAvailableMinor(TransferBalanceProjection projection) { LastProjection = projection; return projection.NetMinor; }
}

sealed class FakeJournalReader(IReadOnlyList<JournalEntry> journals) : ILedgerJournalReader
{
    public Task<IReadOnlyList<JournalEntry>> ReadAsync(BalanceKey key, CancellationToken cancellationToken = default) => Task.FromResult(journals);
}

sealed class FakeWalletRepository(Wallet wallet) : IWalletRepository
{
    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) => Task.FromResult<Wallet?>(wallet.Id == walletId ? wallet : null);
    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Wallet>>([wallet]);
    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

sealed class FakeJournalRepository : IJournalRepository
{
    public JournalEntry? Posted { get; private set; }
    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) => Task.FromResult(Posted?.CorrelationId == correlationId);
    public Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default) { Posted = journalEntry; return Task.CompletedTask; }
    public Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default) => Task.FromResult(Posted?.Id == journalEntryId ? Posted : null);
    public Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) => Task.FromResult(Posted?.CorrelationId == correlationId ? Posted : null);
}
