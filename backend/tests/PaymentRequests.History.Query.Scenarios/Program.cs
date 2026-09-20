using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
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
var afWal = RecipientReference.FromAfWalId("history.user");
var qr = RecipientReference.FromQrToken("history-opaque-qr");
var page = PaymentRequestPageRequest.Create(1, 25);
var statuses = new[] { PaymentRequestStatus.Pending, PaymentRequestStatus.Paid };

var history = new RecordingHistoryReader();
var wallets = new RecordingWalletReader([walletA, walletB]);
var references = new RecordingReferenceReader([afWal, qr]);
var service = new PaymentRequestHistoryQueryService(history, wallets, references);

var all = await service.ListAsync(new PaymentRequestHistoryQuery(
    userId,
    PaymentRequestHistoryDirection.All,
    statuses,
    page,
    PaymentRequestTemporalOrder.NewestFirst));
Assert(history.Calls == 1, "All history must call the history reader once.");
Assert(wallets.Calls == 1 && references.Calls == 1, "All history must resolve wallets and recipient references.");
Assert(history.LastQuery?.Direction == PaymentRequestHistoryDirection.All, "All direction must be forwarded.");
Assert(history.LastQuery?.OwnedWalletIds.Count == 2, "Owned wallets must be forwarded.");
Assert(history.LastQuery?.OwnedRecipientReferences.Count == 2, "Owned recipient references must be forwarded.");
Assert(all.PageNumber == page.PageNumber && all.PageSize == page.PageSize, "Page metadata must be preserved.");

var sentHistory = new RecordingHistoryReader();
var sentWallets = new RecordingWalletReader([walletA]);
var sentReferences = new RecordingReferenceReader([afWal]);
var sentService = new PaymentRequestHistoryQueryService(sentHistory, sentWallets, sentReferences);
await sentService.ListAsync(new PaymentRequestHistoryQuery(
    userId,
    PaymentRequestHistoryDirection.Sent,
    null,
    PaymentRequestPageRequest.Create()));
Assert(sentHistory.LastQuery?.Direction == PaymentRequestHistoryDirection.Sent, "Sent direction must be forwarded.");
Assert(sentReferences.Calls == 0, "Sent history must not resolve recipient references unnecessarily.");

var receivedHistory = new RecordingHistoryReader();
var receivedService = new PaymentRequestHistoryQueryService(
    receivedHistory,
    new RecordingWalletReader([]),
    new RecordingReferenceReader([afWal]));
await receivedService.ListAsync(new PaymentRequestHistoryQuery(
    userId,
    PaymentRequestHistoryDirection.Received,
    null,
    PaymentRequestPageRequest.Create(),
    PaymentRequestTemporalOrder.OldestFirst));
Assert(receivedHistory.LastQuery?.Direction == PaymentRequestHistoryDirection.Received, "Received direction must be forwarded.");
Assert(receivedHistory.LastQuery?.Order == PaymentRequestTemporalOrder.OldestFirst, "Temporal order must be forwarded.");

var emptyReader = new RecordingHistoryReader();
var emptyService = new PaymentRequestHistoryQueryService(
    emptyReader,
    new RecordingWalletReader([]),
    new RecordingReferenceReader([]));
var empty = await emptyService.ListAsync(new PaymentRequestHistoryQuery(
    userId,
    PaymentRequestHistoryDirection.All,
    null,
    PaymentRequestPageRequest.Create(0, 10)));
Assert(empty.TotalCount == 0 && empty.Items.Count == 0 && !empty.HasMore, "Empty ownership must return an empty page.");
Assert(emptyReader.Calls == 0, "Empty ownership must short-circuit the history reader.");

await AssertThrowsAsync<ArgumentException>(
    () => service.ListAsync(new PaymentRequestHistoryQuery(
        Guid.Empty,
        PaymentRequestHistoryDirection.All,
        null,
        PaymentRequestPageRequest.Create())),
    "Empty authenticated user id must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ListAsync(new PaymentRequestHistoryQuery(
        userId,
        (PaymentRequestHistoryDirection)999,
        null,
        PaymentRequestPageRequest.Create())),
    "Unsupported history direction must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ListAsync(new PaymentRequestHistoryQuery(
        userId,
        PaymentRequestHistoryDirection.All,
        [(PaymentRequestStatus)999],
        PaymentRequestPageRequest.Create())),
    "Unsupported status filters must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.ListAsync(new PaymentRequestHistoryQuery(
        userId,
        PaymentRequestHistoryDirection.All,
        null,
        PaymentRequestPageRequest.Create()), cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-REQUEST-HISTORY-1 query foundation scenarios: PASS");

sealed class RecordingHistoryReader : IPaymentRequestHistoryReader
{
    public int Calls { get; private set; }
    public PaymentRequestHistoryReadQuery? LastQuery { get; private set; }

    public Task<PaymentRequestHistoryPage> ListAsync(
        PaymentRequestHistoryReadQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastQuery = query;
        return Task.FromResult(new PaymentRequestHistoryPage(
            [], query.Page.PageNumber, query.Page.PageSize, 0, false));
    }
}

sealed class RecordingWalletReader(IReadOnlyCollection<WalletId> wallets) : IPaymentRequestOwnedWalletReader
{
    public int Calls { get; private set; }

    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(wallets);
    }
}

sealed class RecordingReferenceReader(IReadOnlyCollection<RecipientReference> references)
    : IPaymentRequestOwnedRecipientReferenceReader
{
    public int Calls { get; private set; }

    public Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(references);
    }
}
