using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
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
Assert(!repository.LastSent.RequesterWalletIds.Contains(foreignWallet), "Outbox must not contain arbitrary foreign wallets.");
Assert(walletReader.LastUserId == userId, "Outbox scope must be derived from authenticated user id.");

var emptyRepository = new RecordingQueryRepository();
var emptyWalletReader = new FixedOwnedWalletReader([]);
var emptyReferenceReader = new FixedOwnedReferenceReader([]);
var emptyService = new AuthorizedPaymentRequestQueryService(
    emptyRepository,
    emptyWalletReader,
    emptyReferenceReader);

var emptyInbox = await emptyService.ListInboxAsync(new AuthorizedInboxQuery(
    Guid.NewGuid(), null, PaymentRequestPageRequest.Create()));
var emptyOutbox = await emptyService.ListOutboxAsync(new AuthorizedOutboxQuery(
    Guid.NewGuid(), null, PaymentRequestPageRequest.Create(2, 10)));
Assert(emptyInbox.TotalCount == 0 && emptyInbox.Items.Count == 0 && !emptyInbox.HasMore, "Identity-less inbox must fail closed as an empty page.");
Assert(emptyOutbox.TotalCount == 0 && emptyOutbox.PageNumber == 2 && emptyOutbox.PageSize == 10, "Wallet-less outbox must fail closed and preserve page metadata.");
Assert(emptyRepository.ReceivedCalls == 0 && emptyRepository.SentCalls == 0, "Empty authorization scope must not query persistence.");
Assert(emptyWalletReader.Calls == 2, "Inbox and outbox must resolve owned-wallet scope before short-circuiting.");
Assert(emptyReferenceReader.Calls == 1, "Only inbox must resolve owned recipient references.");

AssertThrows<ArgumentOutOfRangeException>(
    () => PaymentRequestPageRequest.Create(-1, 20),
    "Negative page number must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => PaymentRequestPageRequest.Create(0, 0),
    "Zero page size must be rejected.");
AssertThrows<ArgumentOutOfRangeException>(
    () => PaymentRequestPageRequest.Create(0, 101),
    "Page size above the supported maximum must be rejected.");
await AssertThrowsAsync<ArgumentNullException>(
    () => service.ListInboxAsync(new AuthorizedInboxQuery(userId, null, null!)),
    "Null pagination must be rejected before authorization reads.");

await AssertThrowsAsync<ArgumentException>(
    () => service.ListInboxAsync(new AuthorizedInboxQuery(Guid.Empty, null, PaymentRequestPageRequest.Create())),
    "Empty authenticated user id must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => service.ListOutboxAsync(new AuthorizedOutboxQuery(
        Guid.NewGuid(),
        [(PaymentRequestStatus)999],
        PaymentRequestPageRequest.Create())),
    "Unsupported status filter must be rejected before persistence.");

using var propagationCts = new CancellationTokenSource();
var propagationToken = propagationCts.Token;
var propagationRepository = new RecordingQueryRepository();
var propagationWalletReader = new FixedOwnedWalletReader([ownedWalletA]);
var propagationReferenceReader = new FixedOwnedReferenceReader([afWal]);
var propagationService = new AuthorizedPaymentRequestQueryService(
    propagationRepository,
    propagationWalletReader,
    propagationReferenceReader);

await propagationService.ListInboxAsync(
    new AuthorizedInboxQuery(userId, null, PaymentRequestPageRequest.Create()),
    propagationToken);

Assert(propagationWalletReader.LastCancellationToken == propagationToken, "Inbox must propagate CancellationToken to owned-wallet reader.");
Assert(propagationReferenceReader.LastCancellationToken == propagationToken, "Inbox must propagate CancellationToken to owned-reference reader.");
Assert(propagationRepository.LastReceivedCancellationToken == propagationToken, "Inbox must propagate CancellationToken to received repository.");

await propagationService.ListOutboxAsync(
    new AuthorizedOutboxQuery(userId, null, PaymentRequestPageRequest.Create()),
    propagationToken);

Assert(propagationWalletReader.LastCancellationToken == propagationToken, "Outbox must propagate CancellationToken to owned-wallet reader.");
Assert(propagationRepository.LastSentCancellationToken == propagationToken, "Outbox must propagate CancellationToken to sent repository.");

using var cancelledCts = new CancellationTokenSource();
cancelledCts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.ListInboxAsync(new AuthorizedInboxQuery(userId, null, PaymentRequestPageRequest.Create()), cancelledCts.Token),
    "Cancellation must stop orchestration before authorization reads.");

Console.WriteLine("AFW-BE-REQUEST-INBOX-1 authorized query scenarios: PASS");

sealed class RecordingQueryRepository : IPaymentRequestQueryRepository
{
    public int ReceivedCalls { get; private set; }
    public int SentCalls { get; private set; }
    public ReceivedPaymentRequestsQuery? LastReceived { get; private set; }
    public SentPaymentRequestsQuery? LastSent { get; private set; }
    public CancellationToken LastReceivedCancellationToken { get; private set; }
    public CancellationToken LastSentCancellationToken { get; private set; }

    public Task<PaymentRequestQueryPage> ListReceivedAsync(ReceivedPaymentRequestsQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReceivedCalls++;
        LastReceived = query;
        LastReceivedCancellationToken = cancellationToken;
        return Task.FromResult(new PaymentRequestQueryPage([], query.Page.PageNumber, query.Page.PageSize, 0, false));
    }

    public Task<PaymentRequestQueryPage> ListSentAsync(SentPaymentRequestsQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SentCalls++;
        LastSent = query;
        LastSentCancellationToken = cancellationToken;
        return Task.FromResult(new PaymentRequestQueryPage([], query.Page.PageNumber, query.Page.PageSize, 0, false));
    }
}

sealed class FixedOwnedWalletReader(IReadOnlyCollection<WalletId> wallets) : IPaymentRequestOwnedWalletReader
{
    public int Calls { get; private set; }
    public Guid? LastUserId { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<IReadOnlyCollection<WalletId>> ListOwnedWalletIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastUserId = userId;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(wallets);
    }
}

sealed class FixedOwnedReferenceReader(IReadOnlyCollection<RecipientReference> references) : IPaymentRequestOwnedRecipientReferenceReader
{
    public int Calls { get; private set; }
    public Guid? LastUserId { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<IReadOnlyCollection<RecipientReference>> ListOwnedRecipientReferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastUserId = userId;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(references);
    }
}
