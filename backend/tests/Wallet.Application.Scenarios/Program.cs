using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

await RunAsync("create/get/list wallet", async () =>
{
    var ownerId = Guid.NewGuid();
    var store = new InMemoryWalletRepository();
    var service = new WalletRegistryApplicationService(store, new FixedCurrencyPolicy("XAF", "EUR"));
    var created = await service.CreateAsync(new(ownerId, "xaf", "cm"), DateTimeOffset.UtcNow);
    Assert(created.Succeeded && created.Value is not null, "Wallet creation must succeed.");
    Assert(created.Value!.CurrencyCode == "XAF" && created.Value.CountryCode == "CM", "Codes must be normalized.");
    var fetched = await service.GetAsync(created.Value.WalletId);
    Assert(fetched.Succeeded, "Created wallet must be retrievable.");
    var listed = await service.ListByOwnerAsync(ownerId);
    Assert(listed.Succeeded && listed.Value!.Count == 1, "Owner wallet list must contain created wallet.");
});

await RunAsync("duplicate currency wallet is rejected", async () =>
{
    var ownerId = Guid.NewGuid();
    var service = new WalletRegistryApplicationService(new InMemoryWalletRepository(), new FixedCurrencyPolicy("XAF"));
    Assert((await service.CreateAsync(new(ownerId, "XAF"), DateTimeOffset.UtcNow)).Succeeded, "First wallet must succeed.");
    var duplicate = await service.CreateAsync(new(ownerId, "xaf"), DateTimeOffset.UtcNow);
    Assert(!duplicate.Succeeded && duplicate.ErrorCode == WalletErrorCode.DuplicateWallet, "Duplicate wallet must be rejected.");
});

await RunAsync("unsupported currency is rejected", async () =>
{
    var service = new WalletRegistryApplicationService(new InMemoryWalletRepository(), new FixedCurrencyPolicy("XAF"));
    var result = await service.CreateAsync(new(Guid.NewGuid(), "USD"), DateTimeOffset.UtcNow);
    Assert(!result.Succeeded && result.ErrorCode == WalletErrorCode.UnsupportedCurrency, "Unsupported currency must be rejected.");
});

await RunAsync("lifecycle transitions persist and closed is terminal", async () =>
{
    var store = new InMemoryWalletRepository();
    var service = new WalletRegistryApplicationService(store, new FixedCurrencyPolicy("EUR"));
    var created = await service.CreateAsync(new(Guid.NewGuid(), "EUR"), DateTimeOffset.UtcNow);
    var walletId = created.Value!.WalletId;
    Assert((await service.SuspendAsync(walletId, DateTimeOffset.UtcNow.AddMinutes(1))).Value!.Status == WalletStatus.Suspended, "Suspend must persist.");
    Assert((await service.ActivateAsync(walletId, DateTimeOffset.UtcNow.AddMinutes(2))).Value!.Status == WalletStatus.Active, "Activate must persist.");
    Assert((await service.CloseAsync(walletId, DateTimeOffset.UtcNow.AddMinutes(3))).Value!.Status == WalletStatus.Closed, "Close must persist.");
    var invalid = await service.ActivateAsync(walletId, DateTimeOffset.UtcNow.AddMinutes(4));
    Assert(!invalid.Succeeded && invalid.ErrorCode == WalletErrorCode.InvalidTransition, "Closed wallet must remain terminal.");
});

Console.WriteLine("Wallet.Application scenarios passed.");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FixedCurrencyPolicy(params string[] codes) : ISupportedCurrencyPolicy
{
    private readonly HashSet<string> _codes = new(codes.Select(code => code.ToUpperInvariant()), StringComparer.Ordinal);
    public bool IsSupported(string currencyCode) => _codes.Contains(currencyCode);
}

sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly Dictionary<Guid, AfriWallet.Wallet.Domain.Wallet> _wallets = [];

    public Task<bool> ExistsAsync(Guid ownerId, string currencyCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_wallets.Values.Any(wallet => wallet.OwnerId == ownerId && wallet.Currency.Code == currencyCode));

    public Task AddAsync(AfriWallet.Wallet.Domain.Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets.Add(wallet.Id.Value, wallet);
        return Task.CompletedTask;
    }

    public Task<AfriWallet.Wallet.Domain.Wallet?> GetAsync(WalletId walletId, CancellationToken cancellationToken = default)
    {
        _wallets.TryGetValue(walletId.Value, out var wallet);
        return Task.FromResult(wallet);
    }

    public Task<IReadOnlyList<AfriWallet.Wallet.Domain.Wallet>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AfriWallet.Wallet.Domain.Wallet>>(_wallets.Values.Where(wallet => wallet.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(AfriWallet.Wallet.Domain.Wallet wallet, CancellationToken cancellationToken = default)
    {
        _wallets[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }
}
