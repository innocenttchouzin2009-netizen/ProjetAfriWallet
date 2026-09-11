using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Wallet.Domain;
using Microsoft.Data.Sqlite;
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

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<PaymentRequestDbContext>()
    .UseSqlite(connection)
    .Options;

await using (var setup = new PaymentRequestDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

var requester = WalletId.From(Guid.NewGuid());
var payer = WalletId.From(Guid.NewGuid());
var createdAt = new DateTimeOffset(2026, 9, 11, 13, 0, 0, TimeSpan.Zero);
var correlationId = Guid.NewGuid();
var request = PaymentRequest.Create(
    requester,
    RecipientReference.FromAfWalId("payer.afwal"),
    Currency.Create("eur"),
    7_500,
    correlationId,
    createdAt,
    createdAt.AddHours(2));

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await repository.AddAsync(request);
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    var byId = await repository.GetAsync(request.Id);
    Assert(byId is not null, "Persisted request must be readable by id.");
    Assert(byId!.Id == request.Id, "Request id must round-trip.");
    Assert(byId.RequesterWalletId == requester, "Requester wallet must round-trip.");
    Assert(byId.PayerReference.Kind == RecipientReferenceKind.AfWalId, "AfWal reference kind must round-trip.");
    Assert(byId.PayerReference.Value == "payer.afwal", "AfWal reference value must round-trip.");
    Assert(byId.Currency.Code == "EUR", "Currency must round-trip normalized.");
    Assert(byId.AmountMinor == 7_500, "Amount must round-trip.");
    Assert(byId.Status == PaymentRequestStatus.Pending, "New persisted request must remain pending.");

    var byCorrelation = await repository.FindByCorrelationIdAsync(correlationId);
    Assert(byCorrelation is not null && byCorrelation.Id == request.Id, "Correlation id lookup must return persisted request.");
}

request.Accept(payer, createdAt.AddMinutes(5));
await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await repository.UpdateAsync(request);
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    var accepted = await repository.GetAsync(request.Id);
    Assert(accepted is not null && accepted.Status == PaymentRequestStatus.Accepted, "Accepted lifecycle state must persist.");
    Assert(accepted!.AcceptedPayerWalletId == payer, "Accepted payer wallet must persist.");
    Assert(accepted.AcceptedAtUtc == createdAt.AddMinutes(5), "Acceptance timestamp must persist.");
}

var transferId = Guid.NewGuid();
request.MarkPaid(transferId, createdAt.AddMinutes(10));
await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await repository.UpdateAsync(request);
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    var paid = await repository.GetAsync(request.Id);
    Assert(paid is not null && paid.Status == PaymentRequestStatus.Paid, "Paid lifecycle state must persist.");
    Assert(paid!.TransferId == transferId, "Transfer id must persist.");
    Assert(paid.ClosedAtUtc == createdAt.AddMinutes(10), "Paid close timestamp must persist.");
}

var qrRequest = PaymentRequest.Create(
    requester,
    RecipientReference.FromQrToken("opaque-qr-token-123"),
    Currency.Create("XAF"),
    2_000,
    Guid.NewGuid(),
    createdAt.AddMinutes(20));

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await repository.AddAsync(qrRequest);
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    var roundTrip = await repository.GetAsync(qrRequest.Id);
    Assert(roundTrip is not null && roundTrip.PayerReference.Kind == RecipientReferenceKind.QrToken, "QR reference kind must round-trip.");
    Assert(roundTrip!.PayerReference.Value == "opaque-qr-token-123", "Opaque QR value must round-trip unchanged.");
}

var duplicateCorrelation = Guid.NewGuid();
var first = PaymentRequest.Create(
    requester,
    RecipientReference.FromAfWalId("first.afwal"),
    Currency.Create("EUR"),
    1_000,
    duplicateCorrelation,
    createdAt.AddMinutes(30));
var second = PaymentRequest.Create(
    requester,
    RecipientReference.FromAfWalId("second.afwal"),
    Currency.Create("EUR"),
    2_000,
    duplicateCorrelation,
    createdAt.AddMinutes(31));

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await repository.AddAsync(first);
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await AssertThrowsAsync<InvalidOperationException>(
        () => repository.AddAsync(second),
        "Duplicate correlation id must be rejected by durable storage.");
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    var missing = await repository.GetAsync(PaymentRequestId.New());
    Assert(missing is null, "Unknown payment request id must return null.");

    var missingCorrelation = await repository.FindByCorrelationIdAsync(Guid.NewGuid());
    Assert(missingCorrelation is null, "Unknown correlation id must return null.");
}

var notPersisted = PaymentRequest.Create(
    requester,
    RecipientReference.FromAfWalId("missing.afwal"),
    Currency.Create("EUR"),
    500,
    Guid.NewGuid(),
    createdAt.AddMinutes(40));
await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await AssertThrowsAsync<InvalidOperationException>(
        () => repository.UpdateAsync(notPersisted),
        "Updating a missing request must fail closed.");
}

using (var cts = new CancellationTokenSource())
{
    cts.Cancel();
    await using var db = new PaymentRequestDbContext(options);
    var repository = new EfPaymentRequestRepository(db);
    await AssertThrowsAsync<OperationCanceledException>(
        () => repository.GetAsync(request.Id, cts.Token),
        "Repository cancellation must propagate.");
}

Console.WriteLine("AFW-BE-REQUEST-1 payment request persistence scenarios: PASS");
