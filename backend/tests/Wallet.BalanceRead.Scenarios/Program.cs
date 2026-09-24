using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using IdentityService.Api.Wallet;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("wallet registry, resolver and balance engine are orchestrated", WalletBalanceIsProjected),
    ("empty ledger returns zero available balance", EmptyLedgerReturnsZero),
    ("wallet registry validation failure is propagated", RegistryValidationFailureIsPropagated),
    ("missing ledger account mapping fails explicitly", MissingLedgerMappingFailsExplicitly)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Wallet balance read scenarios passed: {scenarios.Length}/{scenarios.Length}");

static async Task WalletBalanceIsProjected()
{
    var ownerId = Guid.NewGuid();
    var wallet = CreateWallet(ownerId, "XAF", "CM");
    var accountId = AccountId.New();
    var counterparty = AccountId.New();

    var journals = new[]
    {
        Journal(
            "XAF",
            new LedgerLine(accountId, LedgerSide.Credit, 8_000),
            new LedgerLine(counterparty, LedgerSide.Debit, 8_000)),
        Journal(
            "XAF",
            new LedgerLine(accountId, LedgerSide.Debit, 2_500),
            new LedgerLine(counterparty, LedgerSide.Credit, 2_500))
    };

    var resolver = new FakeWalletLedgerAccountResolver(
        new Dictionary<Guid, AccountId> { [wallet.Id.Value] = accountId });
    var reader = new RecordingLedgerJournalReader(journals);
    var service = Service([wallet], resolver, reader);

    var result = await service.ListByOwnerAsync(ownerId);

    Assert(result.Succeeded, "Expected wallet balance read to succeed.");
    Assert(result.Value is { Count: 1 }, "Expected exactly one wallet balance.");

    var balance = result.Value![0];
    Assert(balance.WalletId == wallet.Id.Value, "Wallet id mismatch.");
    Assert(balance.Currency == "XAF", "Currency mismatch.");
    Assert(balance.AvailableMinor == 5_500, "Available balance must come from Balance Engine net projection.");
    Assert(balance.Status == "ACTIVE", "Wallet status mismatch.");
    Assert(balance.CountryCode == "CM", "Country code mismatch.");
    Assert(resolver.LastWalletId == wallet.Id.Value, "Wallet-to-ledger resolver received the wrong wallet id.");
    Assert(reader.LastKey == new BalanceKey(accountId, "XAF"), "Balance Engine received the wrong balance key.");
}

static async Task EmptyLedgerReturnsZero()
{
    var ownerId = Guid.NewGuid();
    var wallet = CreateWallet(ownerId, "EUR");
    var accountId = AccountId.New();

    var service = Service(
        [wallet],
        new FakeWalletLedgerAccountResolver(
            new Dictionary<Guid, AccountId> { [wallet.Id.Value] = accountId }),
        new RecordingLedgerJournalReader([]));

    var result = await service.ListByOwnerAsync(ownerId);

    Assert(result.Succeeded, "Expected empty-ledger balance read to succeed.");
    Assert(result.Value![0].AvailableMinor == 0, "Empty ledger must project a zero balance.");
}

static async Task RegistryValidationFailureIsPropagated()
{
    var resolver = new FakeWalletLedgerAccountResolver(new Dictionary<Guid, AccountId>());
    var reader = new RecordingLedgerJournalReader([]);
    var service = Service([], resolver, reader);

    var result = await service.ListByOwnerAsync(Guid.Empty);

    Assert(!result.Succeeded, "Empty owner id must fail.");
    Assert(result.ErrorCode == WalletErrorCode.ValidationError, "Expected Wallet Registry validation error.");
    Assert(resolver.LastWalletId is null, "Resolver must not run after Wallet Registry failure.");
    Assert(reader.LastKey is null, "Balance Engine must not run after Wallet Registry failure.");
}

static async Task MissingLedgerMappingFailsExplicitly()
{
    var ownerId = Guid.NewGuid();
    var wallet = CreateWallet(ownerId, "USD");
    var service = Service(
        [wallet],
        new FakeWalletLedgerAccountResolver(new Dictionary<Guid, AccountId>()),
        new RecordingLedgerJournalReader([]));

    try
    {
        await service.ListByOwnerAsync(ownerId);
        throw new InvalidOperationException("Expected missing ledger mapping to fail.");
    }
    catch (InvalidOperationException ex)
    {
        Assert(
            ex.Message.Contains("no resolved Ledger account", StringComparison.Ordinal),
            "Missing mapping failure must identify the ledger-account invariant.");
    }
}

static WalletBalanceReadApplicationService Service(
    IReadOnlyList<AfriWallet.Wallet.Domain.Wallet> wallets,
    IWalletLedgerAccountResolver resolver,
    ILedgerJournalReader journalReader)
{
    var repository = new FakeWalletRepository(wallets);
    var registry = new WalletRegistryApplicationService(repository, new AcceptAllCurrencyPolicy());
    var balanceRead = new LedgerBackedBalanceReadService(journalReader, new BalanceProjectionService());
    return new WalletBalanceReadApplicationService(registry, resolver, balanceRead);
}

static AfriWallet.Wallet.Domain.Wallet CreateWallet(
    Guid ownerId,
    string currencyCode,
    string? countryCode = null) =>
    AfriWallet.Wallet.Domain.Wallet.Create(
        WalletId.New(),
        ownerId,
        Currency.Create(currencyCode),
        countryCode is null ? null : CountryCode.Create(countryCode),
        DateTimeOffset.UtcNow);

static JournalEntry Journal(string currency, params LedgerLine[] lines) =>
    JournalEntry.Create(
        JournalEntryId.New(),
        currency,
        $"WALLET-BAL-{Guid.NewGuid():N}",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        lines);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FakeWalletRepository(IEnumerable<AfriWallet.Wallet.Domain.Wallet> wallets) : IWalletRepository
{
    private readonly List<AfriWallet.Wallet.Domain.Wallet> items = [.. wallets];

    public Task<bool> ExistsAsync(
        Guid ownerId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(items.Any(wallet =>
            wallet.OwnerId == ownerId &&
            string.Equals(wallet.Currency.Code, currencyCode, StringComparison.Ordinal)));

    public Task AddAsync(
        AfriWallet.Wallet.Domain.Wallet wallet,
        CancellationToken cancellationToken = default)
    {
        items.Add(wallet);
        return Task.CompletedTask;
    }

    public Task<AfriWallet.Wallet.Domain.Wallet?> GetAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(items.SingleOrDefault(wallet => wallet.Id == walletId));

    public Task<IReadOnlyList<AfriWallet.Wallet.Domain.Wallet>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AfriWallet.Wallet.Domain.Wallet>>(
            items.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(
        AfriWallet.Wallet.Domain.Wallet wallet,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

sealed class AcceptAllCurrencyPolicy : ISupportedCurrencyPolicy
{
    public bool IsSupported(string currencyCode) => true;
}

sealed class FakeWalletLedgerAccountResolver(IReadOnlyDictionary<Guid, AccountId> mappings)
    : IWalletLedgerAccountResolver
{
    public Guid? LastWalletId { get; private set; }

    public Task<AccountId?> ResolveAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        LastWalletId = walletId;
        return Task.FromResult(mappings.TryGetValue(walletId, out var accountId)
            ? (AccountId?)accountId
            : null);
    }
}

sealed class RecordingLedgerJournalReader(IReadOnlyList<JournalEntry> journalEntries)
    : ILedgerJournalReader
{
    public BalanceKey? LastKey { get; private set; }

    public Task<IReadOnlyList<JournalEntry>> ReadAsync(
        BalanceKey key,
        CancellationToken cancellationToken = default)
    {
        LastKey = key;
        return Task.FromResult(journalEntries);
    }
}
