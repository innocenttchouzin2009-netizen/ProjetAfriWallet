using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

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

var databasePath = Path.Combine(Path.GetTempPath(), $"afw-request-query-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite($"Data Source={databasePath}")
    .Options;

try
{
    await using var db = new PaymentRequestDbContext(options);
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    var repository = new EfPaymentRequestRepository(db);

    var requesterA = WalletId.From(Guid.NewGuid());
    var requesterB = WalletId.From(Guid.NewGuid());
    var payerWallet = WalletId.From(Guid.NewGuid());
    var alice = RecipientReference.FromAfWalId("alice.afwal");
    var bob = RecipientReference.FromAfWalId("bob.afwal");
    var qr = RecipientReference.FromQrToken("opaque-received-token");
    var eur = Currency.Create("EUR");
    var origin = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    var oldest = PaymentRequest.Create(requesterA, alice, eur, 1_000, Guid.NewGuid(), origin);
    var tiedCancelled = PaymentRequest.Create(requesterA, alice, eur, 2_000, Guid.NewGuid(), origin.AddMinutes(10));
    tiedCancelled.Cancel(origin.AddMinutes(11));
    var tiedPending = PaymentRequest.Create(requesterA, alice, eur, 3_000, Guid.NewGuid(), origin.AddMinutes(10));
    var acceptedViaQr = PaymentRequest.Create(requesterB, qr, eur, 4_000, Guid.NewGuid(), origin.AddMinutes(20));
    acceptedViaQr.Accept(payerWallet, origin.AddMinutes(21));
    var unrelated = PaymentRequest.Create(requesterA, bob, eur, 5_000, Guid.NewGuid(), origin.AddMinutes(30));

    foreach (var request in new[] { oldest, tiedCancelled, tiedPending, acceptedViaQr, unrelated })
    {
        await repository.AddAsync(request);
    }

    var sentFirst = await repository.ListSentAsync(new SentPaymentRequestsQuery(
        [requesterA],
        null,
        PaymentRequestPageRequest.Create(0, 2),
        PaymentRequestTemporalOrder.NewestFirst));

    Assert(sentFirst.TotalCount == 4, "Outbox total must include all requester A requests.");
    Assert(sentFirst.Items.Count == 2, "Outbox first page must honor page size.");
    Assert(sentFirst.HasMore, "Outbox first page must report more results.");
    Assert(sentFirst.Items[0].Id == unrelated.Id, "Newest request must be first.");

    var tiedExpected = new[] { tiedCancelled.Id, tiedPending.Id }
        .OrderByDescending(id => id.Value)
        .ToArray();
    Assert(sentFirst.Items[1].Id == tiedExpected[0], "Same-timestamp ordering must use id as deterministic tie-breaker.");

    var sentSecond = await repository.ListSentAsync(new SentPaymentRequestsQuery(
        [requesterA],
        null,
        PaymentRequestPageRequest.Create(1, 2),
        PaymentRequestTemporalOrder.NewestFirst));

    Assert(sentSecond.Items.Count == 2, "Outbox second page must contain the remaining items.");
    Assert(!sentFirst.Items.Select(x => x.Id).Intersect(sentSecond.Items.Select(x => x.Id)).Any(), "Pages must not overlap.");
    Assert(sentSecond.Items[0].Id == tiedExpected[1], "Second tied item must follow deterministic id ordering.");
    Assert(sentSecond.Items[1].Id == oldest.Id, "Oldest request must be last in newest-first order.");
    Assert(!sentSecond.HasMore, "Outbox second page must be terminal.");

    var pendingOnly = await repository.ListSentAsync(new SentPaymentRequestsQuery(
        [requesterA],
        [PaymentRequestStatus.Pending],
        PaymentRequestPageRequest.Create(0, 20)));
    Assert(pendingOnly.TotalCount == 3, "Status filter must keep only pending outbox requests.");
    Assert(pendingOnly.Items.All(item => item.Status == PaymentRequestStatus.Pending), "Status filter leaked another state.");

    var received = await repository.ListReceivedAsync(new ReceivedPaymentRequestsQuery(
        [alice],
        [payerWallet],
        null,
        PaymentRequestPageRequest.Create(0, 20),
        PaymentRequestTemporalOrder.NewestFirst));

    Assert(received.TotalCount == 4, "Inbox must combine authorized recipient references with accepted payer wallets.");
    Assert(received.Items.Any(item => item.Id == acceptedViaQr.Id), "Accepted QR request must remain visible through payer wallet fallback.");
    Assert(received.Items.All(item => item.Id != unrelated.Id), "Unrelated recipient request must not appear in inbox.");
    Assert(received.Items.All(item => item.PayerReferenceKind is RecipientReferenceKind.AfWalId or RecipientReferenceKind.QrToken), "Read model must expose only recipient kind, not raw recipient value.");

    var oldestFirst = await repository.ListReceivedAsync(new ReceivedPaymentRequestsQuery(
        [alice],
        Array.Empty<WalletId>(),
        null,
        PaymentRequestPageRequest.Create(0, 20),
        PaymentRequestTemporalOrder.OldestFirst));
    Assert(oldestFirst.Items[0].Id == oldest.Id, "Oldest-first ordering must be honored.");

    await AssertThrowsAsync<ArgumentException>(
        () => repository.ListReceivedAsync(new ReceivedPaymentRequestsQuery(
            Array.Empty<RecipientReference>(),
            Array.Empty<WalletId>(),
            null,
            PaymentRequestPageRequest.Create())),
        "Inbox query without an authorized selector must fail closed.");

    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await AssertThrowsAsync<OperationCanceledException>(
        () => repository.ListSentAsync(new SentPaymentRequestsQuery(
            [requesterA],
            null,
            PaymentRequestPageRequest.Create()), cts.Token),
        "Query cancellation must propagate.");

    Console.WriteLine("AFW-BE-REQUEST-INBOX-1 query contracts and repository read model scenarios: PASS");
}
finally
{
    if (File.Exists(databasePath)) File.Delete(databasePath);
}
