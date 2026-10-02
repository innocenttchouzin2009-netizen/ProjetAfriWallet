using AfriWallet.TransactionHistory.Infrastructure;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("owned wallet reader returns only authenticated owner wallets", ReturnsOwnedWalletsOnly),
    ("owned wallet reader de-duplicates wallet ids deterministically", DeduplicatesDeterministically),
    ("owned wallet reader rejects empty user id", RejectsEmptyUserId),
    ("owned wallet reader propagates cancellation before repository access", PropagatesCancellation),
    ("owned wallet reader fails closed when repository returns null", NullRepositoryResultFailsClosed)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"Transaction history ownership scenarios passed: {scenarios.Length}/{scenarios.Length}");

static async Task ReturnsOwnedWalletsOnly()
{
    var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var foreignOwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    var ownedA = Wallet(ownerId, "11111111-1111-1111-1111-111111111111");
    var ownedB = Wallet(ownerId, "22222222-2222-2222-2222-222222222222");
    var foreign = Wallet(foreignOwnerId, "33333333-3333-3333-3333-333333333333");

    var repository = new StubWalletRepository([ownedB, foreign, ownedA]);
    var reader = new WalletRepositoryTransactionHistoryOwnedWalletReader(repository);

    var result = await reader.ListOwnedWalletIdsAsync(ownerId);

    Assert(result.SequenceEqual(new[] { ownedA.Id, ownedB.Id }), "Reader must expose only the authenticated owner's wallet ids in stable order.");
    Assert(repository.LastOwnerId == ownerId, "Reader must query the wallet repository with the authenticated owner id.");
}

static async Task DeduplicatesDeterministically()
{
    var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var wallet = Wallet(ownerId, "44444444-4444-4444-4444-444444444444");

    var repository = new StubWalletRepository([wallet, wallet]);
    var reader = new WalletRepositoryTransactionHistoryOwnedWalletReader(repository);

    var result = await reader.ListOwnedWalletIdsAsync(ownerId);

    Assert(result.Count == 1, "Duplicate wallet records must not duplicate transaction-history authorization.");
    Assert(result.Single() == wallet.Id, "The owned wallet id must be preserved.");
}

static async Task RejectsEmptyUserId()
{
    var repository = new StubWalletRepository([]);
    var reader = new WalletRepositoryTransactionHistoryOwnedWalletReader(repository);

    await AssertThrowsAsync<ArgumentException>(() => reader.ListOwnedWalletIdsAsync(Guid.Empty));
    Assert(repository.Calls == 0, "Repository must not be queried for an empty user id.");
}

static async Task PropagatesCancellation()
{
    var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var repository = new StubWalletRepository([]);
    var reader = new WalletRepositoryTransactionHistoryOwnedWalletReader(repository);
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await AssertThrowsAsync<OperationCanceledException>(() =>
        reader.ListOwnedWalletIdsAsync(ownerId, cts.Token));

    Assert(repository.Calls == 0, "Cancellation must be observed before repository access.");
}

static async Task NullRepositoryResultFailsClosed()
{
    var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var repository = new StubWalletRepository([], returnNull: true);
    var reader = new WalletRepositoryTransactionHistoryOwnedWalletReader(repository);

    await AssertThrowsAsync<InvalidOperationException>(() =>
        reader.ListOwnedWalletIdsAsync(ownerId));
}

static Wallet Wallet(Guid ownerId, string walletId) =>
    AfriWallet.Wallet.Domain.Wallet.Create(
        WalletId.From(Guid.Parse(walletId)),
        ownerId,
        Currency.Create("EUR"),
        countryCode: null,
        new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.Zero));

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task AssertThrowsAsync<TException>(Func<Task> action)
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

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

sealed class StubWalletRepository(
    IReadOnlyList<AfriWallet.Wallet.Domain.Wallet> wallets,
    bool returnNull = false) : IWalletRepository
{
    public int Calls { get; private set; }
    public Guid? LastOwnerId { get; private set; }

    public Task<IReadOnlyList<AfriWallet.Wallet.Domain.Wallet>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastOwnerId = ownerId;
        return Task.FromResult(returnNull ? null! : wallets);
    }

    public Task<bool> ExistsAsync(
        Guid ownerId,
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task AddAsync(
        AfriWallet.Wallet.Domain.Wallet wallet,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AfriWallet.Wallet.Domain.Wallet?> GetAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task UpdateAsync(
        AfriWallet.Wallet.Domain.Wallet wallet,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
