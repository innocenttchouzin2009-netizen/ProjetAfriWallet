using AfriWallet.TransactionHistory.Application;
using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
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

    throw new InvalidOperationException(message);
}

var userId = Guid.NewGuid();
var walletA = WalletId.From(Guid.NewGuid());
var walletB = WalletId.From(Guid.NewGuid());
var cursor = new TransactionHistoryCursor(
    new DateTimeOffset(2026, 9, 29, 18, 0, 0, TimeSpan.Zero),
    Guid.NewGuid());
var page = TransactionHistoryPageRequest.Create(25, cursor);

var ownedWallets = new RecordingOwnedWalletReader([walletA, walletB, walletA, default]);
var reader = new RecordingTransactionHistoryReader();
var service = new AuthorizedTransactionHistoryQueryService(reader, ownedWallets);

var result = await service.ListAsync(new TransactionHistoryQuery(userId, page));

Assert(ownedWallets.Calls == 1, "Authorized query must resolve owned wallets exactly once.");
Assert(ownedWallets.LastUserId == userId, "Authorized query must scope ownership by authenticated user id.");
Assert(reader.Calls == 1, "Authorized query must call the transaction history reader once when wallets are owned.");
Assert(reader.LastWalletIds is not null && reader.LastWalletIds.Count == 2, "Authorized query must remove empty and duplicate wallet ids.");
Assert(reader.LastWalletIds!.Contains(walletA) && reader.LastWalletIds.Contains(walletB), "Authorized query must forward only owned wallets.");
Assert(ReferenceEquals(reader.LastPage, page), "Authorized query must forward the validated page request unchanged.");
Assert(result.Items.Count == 0 && result.NextCursor is null, "Reader result must be returned unchanged.");

var emptyReader = new RecordingTransactionHistoryReader();
var emptyOwner = new RecordingOwnedWalletReader([]);
var emptyService = new AuthorizedTransactionHistoryQueryService(emptyReader, emptyOwner);
var empty = await emptyService.ListAsync(
    new TransactionHistoryQuery(userId, TransactionHistoryPageRequest.Create()));

Assert(emptyOwner.Calls == 1, "Empty ownership must still resolve the authenticated user's wallets.");
Assert(emptyReader.Calls == 0, "Empty ownership must short-circuit the transaction history reader.");
Assert(empty.Items.Count == 0 && empty.NextCursor is null, "Empty ownership must return an empty terminal page.");

var invalidOnlyReader = new RecordingTransactionHistoryReader();
var invalidOnlyService = new AuthorizedTransactionHistoryQueryService(
    invalidOnlyReader,
    new RecordingOwnedWalletReader([default, default]));
var invalidOnly = await invalidOnlyService.ListAsync(
    new TransactionHistoryQuery(userId, TransactionHistoryPageRequest.Create()));
Assert(invalidOnlyReader.Calls == 0, "Empty wallet ids must not reach the transaction history reader.");
Assert(invalidOnly.Items.Count == 0 && invalidOnly.NextCursor is null, "Invalid-only ownership must return an empty page.");

await AssertThrowsAsync<ArgumentException>(
    () => service.ListAsync(new TransactionHistoryQuery(
        Guid.Empty,
        TransactionHistoryPageRequest.Create())),
    "Empty authenticated user id must be rejected.");
Assert(ownedWallets.Calls == 1, "Invalid user id must be rejected before ownership lookup.");

await AssertThrowsAsync<ArgumentNullException>(
    () => service.ListAsync(null!),
    "Null query must be rejected.");

await AssertThrowsAsync<ArgumentNullException>(
    () => service.ListAsync(new TransactionHistoryQuery(userId, null!)),
    "Null page request must be rejected.");

var nullOwnerService = new AuthorizedTransactionHistoryQueryService(
    new RecordingTransactionHistoryReader(),
    new NullOwnedWalletReader());
await AssertThrowsAsync<InvalidOperationException>(
    () => nullOwnerService.ListAsync(
        new TransactionHistoryQuery(userId, TransactionHistoryPageRequest.Create())),
    "A null ownership result must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
var cancelledOwner = new RecordingOwnedWalletReader([walletA]);
var cancelledReader = new RecordingTransactionHistoryReader();
var cancelledService = new AuthorizedTransactionHistoryQueryService(cancelledReader, cancelledOwner);
await AssertThrowsAsync<OperationCanceledException>(
    () => cancelledService.ListAsync(
        new TransactionHistoryQuery(userId, TransactionHistoryPageRequest.Create()),
        cts.Token),
    "Cancellation must propagate before ownership or history reads.");
Assert(cancelledOwner.Calls == 0 && cancelledReader.Calls == 0, "Cancelled query must not invoke dependencies.");

Console.WriteLine("AFW-BE-TRANSACTION-HISTORY-1 authorized query service scenarios: PASS");

sealed class RecordingOwnedWalletReader(IReadOnlyCollection<WalletId> wallets)
    : ITransactionHistoryOwnedWalletReader
{
    public int Calls { get; private set; }
    public Guid? LastUserId { get; private set; }

    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastUserId = userId;
        return Task.FromResult(wallets);
    }
}

sealed class NullOwnedWalletReader : ITransactionHistoryOwnedWalletReader
{
    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<WalletId>>(null!);
}

sealed class RecordingTransactionHistoryReader : ITransactionHistoryReader
{
    public int Calls { get; private set; }
    public IReadOnlyCollection<WalletId>? LastWalletIds { get; private set; }
    public TransactionHistoryPageRequest? LastPage { get; private set; }

    public Task<TransactionHistoryPage> ReadAsync(
        IReadOnlyCollection<WalletId> walletIds,
        TransactionHistoryPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastWalletIds = walletIds;
        LastPage = page;
        return Task.FromResult(new TransactionHistoryPage([], null));
    }
}
