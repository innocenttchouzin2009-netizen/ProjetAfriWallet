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

var databasePath = Path.Combine(Path.GetTempPath(), $"afw-request-history-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite($"Data Source={databasePath}")
    .Options;

try
{
    await using var db = new PaymentRequestDbContext(options);
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();

    var repository = new EfPaymentRequestRepository(db);
    var history = new EfPaymentRequestHistoryReader(db);

    var userWallet = WalletId.From(Guid.NewGuid());
    var otherWallet = WalletId.From(Guid.NewGuid());
    var acceptedUserWallet = WalletId.From(Guid.NewGuid());
    var userAfWal = RecipientReference.FromAfWalId("history.user");
    var otherAfWal = RecipientReference.FromAfWalId("history.other");
    var userQr = RecipientReference.FromQrToken("history-user-qr");
    var eur = Currency.Create("EUR");
    var origin = new DateTimeOffset(2026, 9, 17, 18, 0, 0, TimeSpan.Zero);

    var sentOldest = PaymentRequest.Create(
        userWallet,
        otherAfWal,
        eur,
        1_000,
        Guid.NewGuid(),
        origin);

    var receivedByAfWal = PaymentRequest.Create(
        otherWallet,
        userAfWal,
        eur,
        2_000,
        Guid.NewGuid(),
        origin.AddMinutes(10));

    var receivedByAcceptedWallet = PaymentRequest.Create(
        otherWallet,
        RecipientReference.FromQrToken("historical-opaque-reference"),
        eur,
        3_000,
        Guid.NewGuid(),
        origin.AddMinutes(20));
    receivedByAcceptedWallet.Accept(acceptedUserWallet, origin.AddMinutes(21));

    var sentCancelled = PaymentRequest.Create(
        userWallet,
        otherAfWal,
        eur,
        4_000,
        Guid.NewGuid(),
        origin.AddMinutes(30));
    sentCancelled.Cancel(origin.AddMinutes(31));

    var receivedByQr = PaymentRequest.Create(
        otherWallet,
        userQr,
        eur,
        5_000,
        Guid.NewGuid(),
        origin.AddMinutes(40));

    var unrelated = PaymentRequest.Create(
        otherWallet,
        otherAfWal,
        eur,
        6_000,
        Guid.NewGuid(),
        origin.AddMinutes(50));

    foreach (var request in new[]
             {
                 sentOldest,
                 receivedByAfWal,
                 receivedByAcceptedWallet,
                 sentCancelled,
                 receivedByQr,
                 unrelated
             })
    {
        await repository.AddAsync(request);
    }

    var scopeWallets = new[] { userWallet, acceptedUserWallet };
    var scopeReferences = new[] { userAfWal, userQr };

    var allFirstPage = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        scopeWallets,
        scopeReferences,
        PaymentRequestHistoryDirection.All,
        null,
        PaymentRequestPageRequest.Create(0, 2),
        PaymentRequestTemporalOrder.NewestFirst));

    Assert(allFirstPage.TotalCount == 5, "History All must union sent and received scopes only.");
    Assert(allFirstPage.Items.Count == 2, "History page size must be honored.");
    Assert(allFirstPage.HasMore, "First history page must report more results.");
    Assert(allFirstPage.Items[0].Request.Id == receivedByQr.Id, "Newest received request must be first.");
    Assert(allFirstPage.Items[0].Direction == PaymentRequestHistoryDirection.Received, "Newest item direction must be Received.");
    Assert(allFirstPage.Items[1].Request.Id == sentCancelled.Id, "Second newest request must follow temporal ordering.");
    Assert(allFirstPage.Items[1].Direction == PaymentRequestHistoryDirection.Sent, "Sent item direction must be preserved.");

    var allSecondPage = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        scopeWallets,
        scopeReferences,
        PaymentRequestHistoryDirection.All,
        null,
        PaymentRequestPageRequest.Create(1, 2),
        PaymentRequestTemporalOrder.NewestFirst));

    Assert(allSecondPage.Items.Count == 2, "Second history page must honor page size.");
    Assert(!allFirstPage.Items.Select(item => item.Request.Id).Intersect(allSecondPage.Items.Select(item => item.Request.Id)).Any(),
        "History pages must not overlap.");
    Assert(allSecondPage.Items[0].Request.Id == receivedByAcceptedWallet.Id, "Accepted payer wallet fallback must participate in history ordering.");
    Assert(allSecondPage.Items[0].Direction == PaymentRequestHistoryDirection.Received, "Accepted payer wallet item must be Received.");

    var sentOnly = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        scopeWallets,
        scopeReferences,
        PaymentRequestHistoryDirection.Sent,
        null,
        PaymentRequestPageRequest.Create(0, 20)));

    Assert(sentOnly.TotalCount == 2, "Sent history must contain only requests from owned requester wallets.");
    Assert(sentOnly.Items.All(item => item.Direction == PaymentRequestHistoryDirection.Sent), "Sent history leaked another direction.");
    Assert(sentOnly.Items.Select(item => item.Request.Id).ToHashSet().SetEquals([sentOldest.Id, sentCancelled.Id]),
        "Sent history scope is incorrect.");

    var receivedOnly = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        scopeWallets,
        scopeReferences,
        PaymentRequestHistoryDirection.Received,
        null,
        PaymentRequestPageRequest.Create(0, 20)));

    Assert(receivedOnly.TotalCount == 3, "Received history must combine AfWal ID, QR and accepted payer wallet scopes.");
    Assert(receivedOnly.Items.All(item => item.Direction == PaymentRequestHistoryDirection.Received), "Received history leaked another direction.");
    Assert(receivedOnly.Items.All(item => item.Request.Id != unrelated.Id), "Unrelated request leaked into received history.");

    var cancelledOnly = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        scopeWallets,
        scopeReferences,
        PaymentRequestHistoryDirection.All,
        [PaymentRequestStatus.Cancelled],
        PaymentRequestPageRequest.Create(0, 20)));

    Assert(cancelledOnly.TotalCount == 1, "Status filter must be applied before union pagination.");
    Assert(cancelledOnly.Items[0].Request.Id == sentCancelled.Id, "Cancelled filter returned wrong request.");

    var oldestFirst = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        scopeWallets,
        scopeReferences,
        PaymentRequestHistoryDirection.All,
        null,
        PaymentRequestPageRequest.Create(0, 20),
        PaymentRequestTemporalOrder.OldestFirst));

    Assert(oldestFirst.Items[0].Request.Id == sentOldest.Id, "Oldest-first history ordering must be honored.");
    Assert(oldestFirst.Items[^1].Request.Id == receivedByQr.Id, "Newest item must be last in oldest-first order.");

    var empty = await history.ListAsync(new PaymentRequestHistoryReadQuery(
        Array.Empty<WalletId>(),
        Array.Empty<RecipientReference>(),
        PaymentRequestHistoryDirection.All,
        null,
        PaymentRequestPageRequest.Create()));

    Assert(empty.TotalCount == 0 && empty.Items.Count == 0 && !empty.HasMore, "Empty ownership scope must return an empty page.");

    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await AssertThrowsAsync<OperationCanceledException>(
        () => history.ListAsync(new PaymentRequestHistoryReadQuery(
            scopeWallets,
            scopeReferences,
            PaymentRequestHistoryDirection.All,
            null,
            PaymentRequestPageRequest.Create()), cts.Token),
        "History cancellation must propagate.");

    Console.WriteLine("AFW-BE-REQUEST-HISTORY-1 persistence query adapter scenarios: PASS");
}
finally
{
    if (File.Exists(databasePath)) File.Delete(databasePath);
}
