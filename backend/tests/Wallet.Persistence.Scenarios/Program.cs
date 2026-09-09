using AfriWallet.Wallet.Domain;
using AfriWallet.Wallet.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

await RunAsync("initial migration creates wallet registry schema", async () =>
{
    await using var fixture = await WalletFixture.CreateAsync();
    var applied = await fixture.Context.Database.GetAppliedMigrationsAsync();
    Assert(applied.Contains("20260909211000_InitialWalletRegistry"), "Initial wallet migration must be applied.");
});

await RunAsync("repository persists and rehydrates wallet aggregate", async () =>
{
    await using var fixture = await WalletFixture.CreateAsync();
    var repository = new EfWalletRepository(fixture.Context);
    var createdAt = new DateTimeOffset(2026, 9, 9, 21, 0, 0, TimeSpan.Zero);
    var wallet = Wallet.Create(WalletId.New(), Guid.NewGuid(), Currency.Create("xaf"), CountryCode.Create("cm"), createdAt);

    await repository.AddAsync(wallet);
    var reloaded = await repository.GetAsync(wallet.Id);

    Assert(reloaded is not null, "Persisted wallet must be reloaded.");
    Assert(reloaded!.Id == wallet.Id, "Wallet id mismatch after rehydration.");
    Assert(reloaded.OwnerId == wallet.OwnerId, "Owner id mismatch after rehydration.");
    Assert(reloaded.Currency.Code == "XAF", "Currency must remain canonical.");
    Assert(reloaded.CountryCode?.Value == "CM", "Country code must remain canonical.");
    Assert(reloaded.Status == WalletStatus.Active, "New wallet must remain active after rehydration.");
});

await RunAsync("repository persists lifecycle updates", async () =>
{
    await using var fixture = await WalletFixture.CreateAsync();
    var repository = new EfWalletRepository(fixture.Context);
    var createdAt = new DateTimeOffset(2026, 9, 9, 21, 0, 0, TimeSpan.Zero);
    var wallet = Wallet.Create(WalletId.New(), Guid.NewGuid(), Currency.Create("EUR"), null, createdAt);

    await repository.AddAsync(wallet);
    wallet.Suspend(createdAt.AddMinutes(5));
    await repository.UpdateAsync(wallet);

    var reloaded = await repository.GetAsync(wallet.Id);
    Assert(reloaded?.Status == WalletStatus.Suspended, "Suspended status must persist.");
    Assert(reloaded?.UpdatedAtUtc == createdAt.AddMinutes(5), "Updated timestamp must persist.");
});

await RunAsync("database enforces one wallet per owner and currency", async () =>
{
    await using var fixture = await WalletFixture.CreateAsync();
    var repository = new EfWalletRepository(fixture.Context);
    var ownerId = Guid.NewGuid();
    var createdAt = new DateTimeOffset(2026, 9, 9, 21, 0, 0, TimeSpan.Zero);

    await repository.AddAsync(Wallet.Create(WalletId.New(), ownerId, Currency.Create("USD"), null, createdAt));

    try
    {
        await repository.AddAsync(Wallet.Create(WalletId.New(), ownerId, Currency.Create("USD"), null, createdAt.AddSeconds(1)));
        throw new InvalidOperationException("Expected database uniqueness violation.");
    }
    catch (DbUpdateException)
    {
    }
});

Console.WriteLine("Wallet persistence scenarios passed.");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class WalletFixture : IAsyncDisposable
{
    private WalletFixture(SqliteConnection connection, WalletDbContext context)
    {
        Connection = connection;
        Context = context;
    }

    public SqliteConnection Connection { get; }
    public WalletDbContext Context { get; }

    public static async Task<WalletFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new WalletDbContext(options);
        await context.Database.MigrateAsync();
        return new WalletFixture(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
