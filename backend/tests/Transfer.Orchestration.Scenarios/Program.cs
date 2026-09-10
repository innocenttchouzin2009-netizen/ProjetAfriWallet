using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;

await RunAsync("orchestration posts balanced journal", async () =>
{
    var sourceWallet = Guid.NewGuid();
    var targetWallet = Guid.NewGuid();
    var sourceAccount = AccountId.New();
    var targetAccount = AccountId.New();
    var wallets = new FakeWalletReader(
        new(sourceWallet, sourceAccount, "XAF", true),
        new(targetWallet, targetAccount, "XAF", true));
    var balances = new FakeBalanceReader(10_000);
    var ledger = new FakeLedgerPort();
    var service = CreateService(wallets, balances, ledger);
    var correlation = Guid.NewGuid();

    var result = await service.ExecuteAsync(new(sourceWallet, targetWallet, 2_500, correlation, DateTimeOffset.UtcNow));

    Assert(ledger.Posted is not null, "Journal must be posted.");
    Assert(result.JournalEntry.CorrelationId == correlation, "Correlation id mismatch.");
    Assert(result.JournalEntry.Lines.Sum(x => x.Side == LedgerSide.Debit ? x.AmountMinor : 0) == 2_500, "Debit total mismatch.");
    Assert(result.JournalEntry.Lines.Sum(x => x.Side == LedgerSide.Credit ? x.AmountMinor : 0) == 2_500, "Credit total mismatch.");
});

await RunAsync("orchestration reads source balance", async () =>
{
    var source = Guid.NewGuid();
    var target = Guid.NewGuid();
    var sourceAccount = AccountId.New();
    var balances = new FakeBalanceReader(5_000);
    var service = CreateService(
        new FakeWalletReader(new(source, sourceAccount, "EUR", true), new(target, AccountId.New(), "EUR", true)),
        balances,
        new FakeLedgerPort());

    await service.ExecuteAsync(new(source, target, 500, Guid.NewGuid(), DateTimeOffset.UtcNow));
    Assert(balances.LastAccountId == sourceAccount && balances.LastCurrency == "EUR", "Source balance lookup mismatch.");
});

await RunAsync("missing source wallet is rejected", async () =>
{
    var target = Guid.NewGuid();
    var service = CreateService(
        new FakeWalletReader(new(target, AccountId.New(), "XAF", true)),
        new FakeBalanceReader(10_000),
        new FakeLedgerPort());
    await ExpectAsync<InvalidOperationException>(() => service.ExecuteAsync(new(Guid.NewGuid(), target, 100, Guid.NewGuid(), DateTimeOffset.UtcNow)));
});

await RunAsync("insufficient funds prevent posting", async () =>
{
    var source = Guid.NewGuid();
    var target = Guid.NewGuid();
    var ledger = new FakeLedgerPort();
    var service = CreateService(
        new FakeWalletReader(new(source, AccountId.New(), "XAF", true), new(target, AccountId.New(), "XAF", true)),
        new FakeBalanceReader(99),
        ledger);
    await ExpectAsync<InvalidOperationException>(() => service.ExecuteAsync(new(source, target, 100, Guid.NewGuid(), DateTimeOffset.UtcNow)));
    Assert(ledger.Posted is null, "Insufficient funds must not post a journal.");
});

await RunAsync("duplicate correlation is rejected before wallet reads", async () =>
{
    var correlation = Guid.NewGuid();
    var wallets = new FakeWalletReader();
    var ledger = new FakeLedgerPort { ExistingCorrelationId = correlation };
    var service = CreateService(wallets, new FakeBalanceReader(1_000), ledger);
    await ExpectAsync<InvalidOperationException>(() => service.ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid(), 100, correlation, DateTimeOffset.UtcNow)));
    Assert(wallets.ReadCount == 0, "Duplicate correlation should short-circuit before wallet reads.");
});

await RunAsync("cancellation token propagates to every port", async () =>
{
    using var cts = new CancellationTokenSource();
    var token = cts.Token;
    var source = Guid.NewGuid();
    var target = Guid.NewGuid();
    var wallets = new FakeWalletReader(new(source, AccountId.New(), "XAF", true), new(target, AccountId.New(), "XAF", true));
    var balances = new FakeBalanceReader(1_000);
    var ledger = new FakeLedgerPort();
    var service = CreateService(wallets, balances, ledger);
    await service.ExecuteAsync(new(source, target, 100, Guid.NewGuid(), DateTimeOffset.UtcNow), token);
    Assert(wallets.LastToken == token && balances.LastToken == token && ledger.LastToken == token, "Cancellation token must propagate.");
});

Console.WriteLine("Transfer orchestration scenarios passed.");

static InternalTransferOrchestrationService CreateService(ITransferWalletReader wallets, ITransferBalanceReader balances, ITransferLedgerPort ledger) =>
    new(wallets, balances, ledger, new InternalTransferPlanningService());

static async Task RunAsync(string name, Func<Task> action)
{
    await action();
    Console.WriteLine($"PASS: {name}");
}

static async Task ExpectAsync<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FakeWalletReader(params TransferWalletSnapshot[] wallets) : ITransferWalletReader
{
    private readonly Dictionary<Guid, TransferWalletSnapshot> items = wallets.ToDictionary(x => x.WalletId);
    public int ReadCount { get; private set; }
    public CancellationToken LastToken { get; private set; }
    public Task<TransferWalletSnapshot?> GetAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        LastToken = cancellationToken;
        items.TryGetValue(walletId, out var wallet);
        return Task.FromResult(wallet);
    }
}

sealed class FakeBalanceReader(long available) : ITransferBalanceReader
{
    public AccountId LastAccountId { get; private set; }
    public string? LastCurrency { get; private set; }
    public CancellationToken LastToken { get; private set; }
    public Task<long> GetAvailableMinorAsync(AccountId accountId, string currencyCode, CancellationToken cancellationToken = default)
    {
        LastAccountId = accountId;
        LastCurrency = currencyCode;
        LastToken = cancellationToken;
        return Task.FromResult(available);
    }
}

sealed class FakeLedgerPort : ITransferLedgerPort
{
    public Guid? ExistingCorrelationId { get; init; }
    public JournalEntry? Posted { get; private set; }
    public CancellationToken LastToken { get; private set; }
    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        return Task.FromResult(ExistingCorrelationId == correlationId);
    }
    public Task PostAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        Posted = journalEntry;
        return Task.CompletedTask;
    }
}
