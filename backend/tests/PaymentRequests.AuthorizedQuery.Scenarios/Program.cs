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
var ownedWalletA = WalletId.From(Guid.NewGuid());
var ownedWalletB = WalletId.From(Guid.NewGuid());
var foreignWallet = WalletId.From(Guid.NewGuid());
var afWal = RecipientReference.FromAfWalId("owner.afwal");
var qr = RecipientReference.FromQrToken("opaque-owned-qr");
var page = PaymentRequestPageRequest.Create(1, 25);
var statuses = new[] { PaymentRequestStatus.Pending, PaymentRequestStatus.Accepted };

var repository = new RecordingQueryRepository();
var walletReader = new FixedOwnedWalletReader([ownedWalletA, ownedWalletB, ownedWalletA]);
var referenceReader = new FixedOwnedReferenceReader([afWal, qr, afWal]);
var service = new AuthorizedPaymentRequestQueryService(repository, walletReader, referenceReader);

await service.ListInboxAsync(new AuthorizedInboxQuery(
    userId,
    statuses,
    page,
    PaymentRequestTemporalOrder.OldestFirst));

Assert(repository.ReceivedCalls == 1, "Inbox must call received read repository exactly once.");
Assert(repository.LastReceived is not null, "Inbox query must be captured.");
Assert(repository.LastReceived!.RecipientWalletIds.Count == 2, "Inbox must deduplicate owned wallets.");
Assert(repository.LastReceived.RecipientWalletIds.Contains(ownedWalletA) && repository.LastReceived.RecipientWalletIds.Contains(ownedWalletB), "Inbox must include only owned wallets.");
Assert(!repository.LastReceived.RecipientWalletIds.Contains(foreignWallet), "Inbox must not contain arbitrary foreign wallets.");
Assert(repository.LastReceived.RecipientReferences.Count == 2, "Inbox must deduplicate owned recipient references.");
Assert(repository.LastReceived.RecipientReferences.Contains(afWal) && repository.LastReceived.RecipientReferences.Contains(qr), "Inbox must include owned AfWal ID and QR references.");
Assert(repository.LastReceived.Page == page, "Inbox pagination must be forwarded unchanged.");
Assert(repository.LastReceived.Order == PaymentRequestTemporalOrder.OldestFirst, "Inbox order must be forwarded unchanged.");
Assert(repository.LastReceived.Statuses?.SequenceEqual(statuses) == true, "Inbox status filter must be forwarded unchanged.");
Assert(walletReader.LastUserId == userId && referenceReader.LastUserId == userId, "Inbox scope must be derived from authenticated user id.");

await service.ListOutboxAsync(new AuthorizedOutboxQuery(
    userId,
    [PaymentRequestStatus.Paid],
    PaymentRequestPageRequest.Create(),
    PaymentRequestTemporalOrder.NewestFirst));

Assert(repository.SentCalls == 1, "Outbox must call sent read repository exactly once.");
Assert(repository.LastSent is not null && repository.LastSent.RequesterWalletIds.Count == 2, "Outbox must use deduplicated owned requester wallets.");
Assert(repository.LastSent!.RequesterWalletIds.Contains(ownedWalletA) && repository.LastSent.RequesterWalletIds.Contains(ownedWalletB), "Outbox must be constrained to owned wallets.");

var emptyRepository = new RecordingQueryRepository();
var emptyService = new AuthorizedPaymentRequestQueryService(
    emptyRepository,
    new FixedOwnedWalletReader([]),
    new FixedOwnedReferenceReader([]));

var emptyInbox = await emptyService.ListInboxAsync(new AuthorizedInboxQuery(
    Guid.NewGuid(), null, PaymentRequestPageRequest.Create()));
var emptyOutbox = await emptyService.ListOutboxAsync(new AuthorizedOutboxQuery(
    Guid.NewGuid(), null, PaymentRequestPageRequest.Create(2, 10)));
Assert(emptyInbox.TotalCount == 0 && emptyInbox.Items.Count == 0 && !emptyInbox.HasMore, "Identity-less inbox must fail closed as an empty page.");
Assert(emptyOutbox.TotalCount == 0 && emptyOutbox.PageNumber == 2 && emptyOutbox.PageSize == 10, "Wallet-less outbox must fail closed and preserve page metadata.");
Assert(emptyRepository.ReceivedCalls == 0 && emptyRepository.SentCalls == 0, "Empty authorization scope must not query persistence.");

await AssertThrowsAsync<ArgumentException>(
    () => service.ListInboxAsync(new AuthorizedInboxQuery(Guid.Empty, null, PaymentRequestPageRequest.Create())),
    "Empty authenticated user id must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ListOutboxAsync(new AuthorizedOutboxQuery(
        Guid.NewGuid(),
        [(PaymentRequestStatus)999],
        PaymentRequestPageRequest.Create())),
    "Unsupported status filter must be rejected before persistence.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.ListInboxAsync(new AuthorizedInboxQuery(userId, null, PaymentRequestPageRequest.Create()), cts.Token),
    "Cancellation must propagate before authorization reads.");

Console.WriteLine("AFW-BE-REQUEST-INBOX-1 authorized inbox/outbox orchestration scenarios: PASS");

sealed class RecordingQueryRepository : IPaymentRequestQueryRepository
{
    public int ReceivedCalls { get; private set; }
    public int SentCalls { get; private set; }
    public ReceivedPaymentRequestsQuery? LastReceived { get; private set; }
    public SentPaymentRequestsQuery? LastSent { get; private set; }

    public Task<PaymentRequestQueryPage> ListReceivedAsync(ReceivedPaymentRequestsQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReceivedCalls++;
        LastReceived = query;
        return Task.FromResult(new PaymentRequestQueryPage([], query.Page.PageNumber, query.Page.PageSize, 0, false));
    }

    public Task<PaymentRequestQueryPage> ListSentAsync(SentPaymentRequestsQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SentCalls++;
        LastSent = query;
        return Task.FromResult(new PaymentRequestQueryPage([], query.Page.PageNumber, query.Page.PageSize, 0, false));
    }
}

sealed class FixedOwnedWalletReader(IReadOnlyCollection<WalletId> wallets) : IPaymentRequestOwnedWalletReader
{
    public Guid? LastUserId { get; private set; }

    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastUserId = userId;
        return Task.FromResult(wallets);
    }
}

sealed class FixedOwnedReferenceReader(IReadOnlyCollection<RecipientReference> references) : IPaymentRequestOwnedRecipientReferenceReader
{
    public Guid? LastUserId { get; private set; }

    public Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastUserId = userId;
        return Task.FromResult(references);
    }
}
