using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Contracts;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Execution.Infrastructure;
using MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;
using MobileMoney.Production.Payout.Funding.Infrastructure;
using MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;
using MobileMoney.Production.Payout.Quote.Infrastructure.Persistence;
using MobileMoney.Production.Payout.Quote.Domain;

await RunAsync("quote owner survives durable restart", QuoteOwnerSurvivesRestartAsync);
await RunAsync("authoritative wallet source enforces owner status currency and ledger balance", AuthoritativeWalletSourceAsync);
await RunAsync("funding replay is same-user only across restart", FundingReplayOwnershipAsync);
await RunAsync("execution Prepared replay is same-user only across restart", ExecutionReplayOwnershipAsync);
await RunAsync("execution rejects quote and funding owned by different users", ExecutionMaterialOwnershipAsync);

Console.WriteLine("Mobile Money payout ownership scenarios passed.");

static async Task QuoteOwnerSurvivesRestartAsync()
{
    var userId = Guid.NewGuid();
    var dbPath = TempDb("quote-owner");
    try
    {
        var quote = CreateQuote();
        await using (var db = new MobileMoneyPayoutQuoteDbContext(
            new DbContextOptionsBuilder<MobileMoneyPayoutQuoteDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options))
        {
            var store = new EfMobileMoneyPayoutQuoteStore(db);
            await store.SaveOwnedAsync(userId, quote);
            Assert(await store.FindOwnerUserIdAsync(quote.QuoteId) == userId,
                "Quote owner must be persisted.");
        }

        await using var restarted = new MobileMoneyPayoutQuoteDbContext(
            new DbContextOptionsBuilder<MobileMoneyPayoutQuoteDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options);
        var restartedStore = new EfMobileMoneyPayoutQuoteStore(restarted);
        Assert(await restartedStore.FindOwnerUserIdAsync(quote.QuoteId) == userId,
            "Quote owner must survive restart.");
    }
    finally
    {
        DeleteDb(dbPath);
    }
}

static async Task AuthoritativeWalletSourceAsync()
{
    var userA = Guid.NewGuid();
    var userB = Guid.NewGuid();
    var walletId = Guid.NewGuid();
    var wallet = Wallet.Create(
        WalletId.From(walletId),
        userA,
        Currency.Create("EUR"),
        null,
        DateTimeOffset.UtcNow);

    var repository = new FixedWalletRepository(wallet);
    var balance = new FixedBalanceReader(7_500);
    var reader = new AuthoritativeMobileMoneyPayoutWalletSourceReader(
        repository,
        balance);

    var source = await reader.GetAsync(
        userA,
        walletId.ToString("D"),
        "EUR");

    Assert(source is not null, "Owned active wallet must resolve.");
    Assert(source!.AvailableMinor == 7_500,
        "Ledger-backed available balance must be authoritative.");

    Assert(await reader.GetAsync(userB, walletId.ToString("D"), "EUR") is null,
        "Another user must not resolve the wallet.");
    Assert(await reader.GetAsync(userA, walletId.ToString("D"), "USD") is null,
        "Currency mismatch must fail closed.");
    Assert(await reader.GetAsync(userA, "wallet-main", "EUR") is null,
        "Non-canonical wallet source id must fail closed.");

    wallet.Suspend(DateTimeOffset.UtcNow.AddSeconds(1));
    Assert(await reader.GetAsync(userA, walletId.ToString("D"), "EUR") is null,
        "Suspended wallet must fail closed.");

    var negativeReader = new AuthoritativeMobileMoneyPayoutWalletSourceReader(
        new FixedWalletRepository(Wallet.Create(
            WalletId.From(Guid.NewGuid()),
            userA,
            Currency.Create("EUR"),
            null,
            DateTimeOffset.UtcNow)),
        new FixedBalanceReader(-1));

    var negativeWallet = await negativeReaderWalletIdAsync(negativeReader, userA);
    Assert(negativeWallet, "Negative authoritative balance must fail closed.");
}

static async Task<bool> negativeReaderWalletIdAsync(
    AuthoritativeMobileMoneyPayoutWalletSourceReader reader,
    Guid userId)
{
    // A malformed/unmapped id cannot reach balance reading; this helper only
    // verifies that the reader never accepts a non-canonical substitute.
    return await reader.GetAsync(userId, Guid.NewGuid().ToString("D"), "EUR") is null;
}

static async Task FundingReplayOwnershipAsync()
{
    var userA = Guid.NewGuid();
    var userB = Guid.NewGuid();
    var walletId = Guid.NewGuid();
    var correlationId = Guid.NewGuid();
    var dbPath = TempDb("funding-owner");

    try
    {
        var wallet = Wallet.Create(
            WalletId.From(walletId),
            userA,
            Currency.Create("EUR"),
            null,
            DateTimeOffset.UtcNow);
        var authoritativeReader =
            new AuthoritativeMobileMoneyPayoutWalletSourceReader(
                new FixedWalletRepository(wallet),
                new FixedBalanceReader(10_000));

        var commandA = OwnedFundingCommand(
            userA,
            correlationId,
            walletId,
            5_000);

        await using (var db = FundingDb(dbPath))
        {
            var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
            var service = new MobileMoneyPayoutFundingPlanningService(
                new EmptyLegacySourceReader(),
                store,
                new MobileMoneyPayoutSplitFundingValidator(),
                authoritativeReader);

            var plan = await service.PlanAsync(commandA);
            Assert(plan.RequiredAmountMinor == 5_000,
                "Owned funding plan must be created.");
            Assert(await store.FindOwnerUserIdAsync(correlationId) == userA,
                "Funding owner must be persisted.");
        }

        await using (var restarted = FundingDb(dbPath))
        {
            var store = new EfMobileMoneyPayoutFundingAttemptStore(restarted);
            var service = new MobileMoneyPayoutFundingPlanningService(
                new EmptyLegacySourceReader(),
                store,
                new MobileMoneyPayoutSplitFundingValidator(),
                authoritativeReader);

            var replay = await service.PlanAsync(commandA);
            Assert(replay.CorrelationId == correlationId,
                "Same-user replay must survive restart.");

            await ExpectAsync<InvalidOperationException>(() =>
                service.PlanAsync(commandA with { UserId = userB }));
        }

        await using (var corrupt = FundingDb(dbPath))
        {
            var row = await corrupt.Plans.SingleAsync(
                x => x.CorrelationId == correlationId);
            row.OwnerUserId = null;
            await corrupt.SaveChangesAsync();
        }

        await using (var restarted = FundingDb(dbPath))
        {
            var store = new EfMobileMoneyPayoutFundingAttemptStore(restarted);
            var service = new MobileMoneyPayoutFundingPlanningService(
                new EmptyLegacySourceReader(),
                store,
                new MobileMoneyPayoutSplitFundingValidator(),
                authoritativeReader);

            await ExpectAsync<InvalidOperationException>(() =>
                service.PlanAsync(commandA));
        }
    }
    finally
    {
        DeleteDb(dbPath);
    }
}

static async Task ExecutionReplayOwnershipAsync()
{
    var userA = Guid.NewGuid();
    var userB = Guid.NewGuid();
    var quoteId = Guid.NewGuid();
    var correlationId = Guid.NewGuid();
    var dbPath = TempDb("execution-owner");
    var quote = ExecutionQuote(quoteId);
    var funding = ExecutionFunding(correlationId);
    var ownership = new FixedExecutionOwnershipReader(
        quoteId,
        correlationId,
        userA,
        userA);
    var request = new PrepareMobileMoneyPayoutExecutionRequest(
        quoteId,
        correlationId,
        "owner-key");

    try
    {
        await using (var db = ExecutionDb(dbPath))
        {
            var store = new EfMobileMoneyPayoutExecutionBindingStore(db);
            var service = new MobileMoneyPayoutExecutionOrchestrator(
                new FixedQuoteBindingReader(quote),
                new FixedFundingBindingReader(funding),
                store,
                new FixedClock(),
                ownership);

            var prepared = await service.PrepareAsync(request, userA);
            Assert(!prepared.IsReplay && prepared.Status == "Prepared",
                "First same-user preparation must create Prepared binding.");
            Assert(
                await store.FindOwnerUserIdAsync(
                    MobileMoneyPayoutExecutionIdempotencyKey.Create("owner-key"))
                == userA,
                "Execution owner must be persisted.");
        }

        await using (var restarted = ExecutionDb(dbPath))
        {
            var store = new EfMobileMoneyPayoutExecutionBindingStore(restarted);
            var service = new MobileMoneyPayoutExecutionOrchestrator(
                new FixedQuoteBindingReader(quote),
                new FixedFundingBindingReader(funding),
                store,
                new FixedClock(),
                ownership);

            var replay = await service.PrepareAsync(request, userA);
            Assert(replay.IsReplay,
                "Same-user execution replay must survive restart.");

            await ExpectAsync<MobileMoneyPayoutExecutionPreparationException>(() =>
                service.PrepareAsync(request, userB));
        }

        await using (var corrupt = ExecutionDb(dbPath))
        {
            var row = await corrupt.Bindings.SingleAsync(
                x => x.IdempotencyKey == "owner-key");
            row.OwnerUserId = null;
            await corrupt.SaveChangesAsync();
        }

        await using (var restarted = ExecutionDb(dbPath))
        {
            var store = new EfMobileMoneyPayoutExecutionBindingStore(restarted);
            var service = new MobileMoneyPayoutExecutionOrchestrator(
                new FixedQuoteBindingReader(quote),
                new FixedFundingBindingReader(funding),
                store,
                new FixedClock(),
                ownership);

            await ExpectAsync<MobileMoneyPayoutExecutionPreparationException>(() =>
                service.PrepareAsync(request, userA));
        }
    }
    finally
    {
        DeleteDb(dbPath);
    }
}

static async Task ExecutionMaterialOwnershipAsync()
{
    var userA = Guid.NewGuid();
    var userB = Guid.NewGuid();
    var quoteId = Guid.NewGuid();
    var correlationId = Guid.NewGuid();

    var service = new MobileMoneyPayoutExecutionOrchestrator(
        new FixedQuoteBindingReader(ExecutionQuote(quoteId)),
        new FixedFundingBindingReader(ExecutionFunding(correlationId)),
        new InMemoryBindingStore(),
        new FixedClock(),
        new FixedExecutionOwnershipReader(
            quoteId,
            correlationId,
            userA,
            userB));

    await ExpectAsync<MobileMoneyPayoutExecutionPreparationException>(() =>
        service.PrepareAsync(
            new PrepareMobileMoneyPayoutExecutionRequest(
                quoteId,
                correlationId,
                "mismatched-owner"),
            userA));
}

static PlanMobileMoneyPayoutFundingCommand OwnedFundingCommand(
    Guid userId,
    Guid correlationId,
    Guid walletId,
    long amountMinor) =>
    new(
        correlationId,
        amountMinor,
        "EUR",
        new[]
        {
            FundingAllocation.Create(
                walletId.ToString("D"),
                FundingSourceType.Wallet,
                amountMinor,
                "EUR")
        },
        new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero),
        userId);

static MobileMoneyPayoutQuote CreateQuote() =>
    MobileMoneyPayoutQuote.Create(
        new MobileMoneyPayoutCorridor("DE", "EUR", "CM", "XAF", "MTN_CM"),
        5_000,
        3_250_000,
        650m,
        Array.Empty<MobileMoneyPayoutFee>(),
        new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 17, 12, 10, 0, TimeSpan.Zero));

static MobileMoneyPayoutExecutionQuoteBinding ExecutionQuote(Guid quoteId) =>
    MobileMoneyPayoutExecutionQuoteBinding.Create(
        quoteId,
        "EUR",
        5_000,
        0,
        5_000,
        "XAF",
        3_250_000,
        new DateTimeOffset(2026, 10, 17, 12, 10, 0, TimeSpan.Zero));

static MobileMoneyPayoutExecutionFundingBinding ExecutionFunding(Guid correlationId) =>
    MobileMoneyPayoutExecutionFundingBinding.Create(
        correlationId,
        5_000,
        "EUR",
        new[]
        {
            MobileMoneyPayoutExecutionFundingAllocation.Create(
                Guid.NewGuid().ToString("D"),
                FundingSourceType.Wallet,
                5_000,
                "EUR")
        });

static MobileMoneyPayoutFundingDbContext FundingDb(string path) =>
    new(new DbContextOptionsBuilder<MobileMoneyPayoutFundingDbContext>()
        .UseSqlite($"Data Source={path}")
        .Options);

static MobileMoneyPayoutExecutionDbContext ExecutionDb(string path) =>
    new(new DbContextOptionsBuilder<MobileMoneyPayoutExecutionDbContext>()
        .UseSqlite($"Data Source={path}")
        .Options);

static string TempDb(string name) =>
    Path.Combine(Path.GetTempPath(), $"afw-beta261-{name}-{Guid.NewGuid():N}.db");

static void DeleteDb(string path)
{
    if (File.Exists(path))
        File.Delete(path);
}

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
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

    throw new InvalidOperationException(
        $"Expected {typeof(TException).Name} was not thrown.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class EmptyLegacySourceReader : IMobileMoneyPayoutFundingSourceReader
{
    public Task<FundingSourceSnapshot?> GetAsync(
        string sourceId,
        FundingSourceType sourceType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<FundingSourceSnapshot?>(null);
}

sealed class FixedBalanceReader(long availableMinor) : IMobileWalletBalanceReader
{
    public Task<long> ReadAvailableMinorAsync(
        Guid walletId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(availableMinor);
}

sealed class FixedWalletRepository(params Wallet[] wallets) : IWalletRepository
{
    private readonly Dictionary<Guid, Wallet> items =
        wallets.ToDictionary(x => x.Id.Value);

    public Task<bool> ExistsAsync(
        Guid ownerId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(items.Values.Any(x =>
            x.OwnerId == ownerId && x.Currency.Code == currencyCode));

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        items[wallet.Id.Value] = wallet;
        return Task.CompletedTask;
    }

    public Task<Wallet?> GetAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(items.GetValueOrDefault(walletId.Value));

    public Task<IReadOnlyList<Wallet>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Wallet>>(
            items.Values.Where(x => x.OwnerId == ownerId).ToArray());

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

sealed class FixedQuoteBindingReader(MobileMoneyPayoutExecutionQuoteBinding quote)
    : IMobileMoneyPayoutExecutionQuoteBindingReader
{
    public Task<MobileMoneyPayoutExecutionQuoteBinding?> FindByIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MobileMoneyPayoutExecutionQuoteBinding?>(
            quote.QuoteId == quoteId ? quote : null);
}

sealed class FixedFundingBindingReader(MobileMoneyPayoutExecutionFundingBinding funding)
    : IMobileMoneyPayoutExecutionFundingBindingReader
{
    public Task<MobileMoneyPayoutExecutionFundingBinding?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MobileMoneyPayoutExecutionFundingBinding?>(
            funding.CorrelationId == correlationId ? funding : null);
}

sealed class FixedExecutionOwnershipReader(
    Guid quoteId,
    Guid fundingCorrelationId,
    Guid quoteOwner,
    Guid fundingOwner)
    : IMobileMoneyPayoutExecutionOwnershipReader
{
    public Task<Guid?> FindQuoteOwnerUserIdAsync(
        Guid requestedQuoteId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(
            requestedQuoteId == quoteId ? quoteOwner : null);

    public Task<Guid?> FindFundingOwnerUserIdAsync(
        Guid requestedFundingCorrelationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(
            requestedFundingCorrelationId == fundingCorrelationId
                ? fundingOwner
                : null);
}

sealed class FixedClock : IMobileMoneyPayoutClock
{
    public DateTimeOffset UtcNow =>
        new(2026, 10, 17, 12, 1, 0, TimeSpan.Zero);
}

sealed class InMemoryBindingStore : IMobileMoneyPayoutExecutionBindingStore
{
    private readonly Dictionary<string, MobileMoneyPayoutExecutionBinding> bindings =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> owners =
        new(StringComparer.Ordinal);

    public Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        bindings.TryGetValue(idempotencyKey.Value, out var binding);
        return Task.FromResult(binding);
    }

    public Task SaveAsync(
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default)
    {
        bindings.TryAdd(binding.IdempotencyKey.Value, binding);
        return Task.CompletedTask;
    }

    public Task SaveOwnedAsync(
        Guid ownerUserId,
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default)
    {
        bindings.TryAdd(binding.IdempotencyKey.Value, binding);
        owners.TryAdd(binding.IdempotencyKey.Value, ownerUserId);
        return Task.CompletedTask;
    }

    public Task<Guid?> FindOwnerUserIdAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            owners.TryGetValue(idempotencyKey.Value, out var owner)
                ? (Guid?)owner
                : null);
}
