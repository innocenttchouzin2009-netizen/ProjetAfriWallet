using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

await RunAsync("empty owner id is rejected", async () =>
{
    var repository = new InMemoryWalletRepository([]);
    var balances = new RecordingBalanceReader([]);
    var service = new MobileWalletReadApplicationService(repository, balances);

    var result = await service.ListAsync(Guid.Empty);

    Assert(!result.Succeeded, "Empty owner id must fail.");
    Assert(result.ErrorCode == WalletErrorCode.ValidationError, "Validation error code expected.");
    Assert(balances.ReadCount == 0, "Balance reader must not be called for invalid owner id.");
});

await RunAsync("owned wallets are mapped to mobile read contract", async () =>
{
    var ownerId = Guid.NewGuid();
    var first = DomainWallet(ownerId, "EUR", "DE");
    var second = DomainWallet(ownerId, "XAF", "CM");
    second.Suspend(DateTimeOffset.UtcNow.AddMinutes(1));

    var repository = new InMemoryWalletRepository([first, second]);
    var balances = new RecordingBalanceReader(new Dictionary<Guid, long>
    {
        [first.Id.Value] = 12_345,
        [second.Id.Value] = 987_654
    });
    var service = new MobileWalletReadApplicationService(repository, balances);

    var result = await service.ListAsync(ownerId);

    Assert(result.Succeeded, "Read must succeed.");
    Assert(result.Value is not null, "Read result is required.");
    Assert(result.Value!.Wallets.Count == 2, "Two wallets expected.");

    var eur = result.Value.Wallets.Single(x => x.WalletId == first.Id.Value);
    Assert(eur.Currency == "EUR", "Currency must be preserved.");
    Assert(eur.AvailableMinor == 12_345, "Available balance must be preserved.");
    Assert(eur.Status == "ACTIVE", "Active status must use stable uppercase representation.");
    Assert(eur.CountryCode == "DE", "Country code must be preserved.");

    var xaf = result.Value.Wallets.Single(x => x.WalletId == second.Id.Value);
    Assert(xaf.Currency == "XAF", "Currency must be preserved.");
    Assert(xaf.AvailableMinor == 987_654, "Available balance must be preserved.");
    Assert(xaf.Status == "SUSPENDED", "Suspended status must use stable uppercase representation.");
    Assert(xaf.CountryCode == "CM", "Country code must be preserved.");
    Assert(balances.ReadCount == 2, "Each returned wallet must have one balance read.");
});

await RunAsync("empty wallet list succeeds", async () =>
{
    var ownerId = Guid.NewGuid();
    var service = new MobileWalletReadApplicationService(
        new InMemoryWalletRepository([]),
        new RecordingBalanceReader([]));

    var result = await service.ListAsync(ownerId);

    Assert(result.Succeeded, "Empty wallet list must still succeed.");
    Assert(result.Value is not null && result.Value.Wallets.Count == 0, "Empty wallet list expected.");
});

Console.WriteLine("Wallet mobile read application scenarios passed.");

static Domain.Wallet DomainWallet(Guid ownerId, string currency, string? countryCode) =>
    Domain.Wallet.Create(
        WalletId.New(),
        ownerId,
        Currency.Create(currency),
        countryCode is null ? null : CountryCode.Create(countryCode),
        DateTimeOffset.UtcNow);

static async Task RunAsync(string name, Func<Task> scenario)
{
    try
    {
        await scenario();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL: {name}: {ex.Message}");
        throw;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class InMemoryWalletRepository(IReadOnlyList<Domain.Wallet> wallets) : IWalletRepository
{
    private readonly List<Domain.Wallet> items = [.. wallets];

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(items.Any(x => x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Domain.Wallet wallet, CancellationToken cancellationToken = default)
    {
        items.Add(wallet);
        return Task.CompletedTask;
    }

    public Task<Domain.Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default) =>
        Task.FromResult(items.SingleOrDefault(x => x.Id == walletId));

    public Task<IReadOnlyList<Domain.Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Domain.Wallet>>(items.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Domain.Wallet wallet, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

sealed class RecordingBalanceReader(IReadOnlyDictionary<Guid, long> balances) : IMobileWalletBalanceReader
{
    public int ReadCount { get; private set; }

    public Task<long> ReadAvailableMinorAsync(
        Guid walletId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult(balances.TryGetValue(walletId, out var value) ? value : 0L);
    }
}
