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
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>().UseSqlite(connection).Options;
await using (var setup = new PaymentRequestDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

var requester = WalletId.From(Guid.NewGuid());
var payer = WalletId.From(Guid.NewGuid());
var t0 = new DateTimeOffset(2026, 9, 14, 20, 0, 0, TimeSpan.Zero);

PaymentRequest NewRequest(string recipient, DateTimeOffset createdAt, DateTimeOffset? expiresAt = null) =>
    PaymentRequest.Create(
        requester,
        RecipientReference.FromAfWalId(recipient),
        Currency.Create("XAF"),
        2_500,
        Guid.NewGuid(),
        createdAt,
        expiresAt);

async Task<IReadOnlyList<PaymentRequestOutboxMessage>> EventsAsync(Guid requestId)
{
    await using var db = new PaymentRequestDbContext(options);
    return await db.PaymentRequestIntegrationOutbox
        .AsNoTracking()
        .Where(x => x.PaymentRequestId == requestId)
        .OrderBy(x => x.OccurredAtUtc)
        .ToArrayAsync();
}

// Created + Accepted + Paid are recorded exactly once.
var paidRequest = NewRequest("payer.secret.afwal", t0, t0.AddHours(1));
await using (var db = new PaymentRequestDbContext(options))
{
    await new EfPaymentRequestRepository(db).AddAsync(paidRequest);
}
var createdEvents = await EventsAsync(paidRequest.Id.Value);
Assert(createdEvents.Count == 1 && createdEvents[0].EventType == PaymentRequestOutboxEventTypes.Created,
    "Created must produce one outbox message.");
Assert(!createdEvents[0].PayloadJson.Contains("payer.secret.afwal", StringComparison.Ordinal),
    "Outbox payload must not contain raw AfWal ID.");

paidRequest.Accept(payer, t0.AddMinutes(1));
await using (var db = new PaymentRequestDbContext(options))
{
    await new EfPaymentRequestRepository(db).UpdateAsync(paidRequest);
}

var transferId = Guid.NewGuid();
paidRequest.MarkPaid(transferId, t0.AddMinutes(2));
await using (var db = new PaymentRequestDbContext(options))
{
    await new EfPaymentRequestRepository(db).UpdateAsync(paidRequest);
}

var paidEvents = await EventsAsync(paidRequest.Id.Value);
Assert(paidEvents.Select(x => x.EventType).SequenceEqual(new[]
{
    PaymentRequestOutboxEventTypes.Created,
    PaymentRequestOutboxEventTypes.Accepted,
    PaymentRequestOutboxEventTypes.Paid
}), "Created, Accepted and Paid must be captured in lifecycle order.");
Assert(paidEvents[^1].PayloadJson.Contains(transferId.ToString(), StringComparison.OrdinalIgnoreCase),
    "Paid payload must contain the transfer id.");
Assert(paidEvents.All(x => x.PublishedAtUtc is null), "Commit 4 must not publish outbox messages.");

// No-op persistence must not duplicate lifecycle events.
await using (var db = new PaymentRequestDbContext(options))
{
    await new EfPaymentRequestRepository(db).UpdateAsync(paidRequest);
}
Assert((await EventsAsync(paidRequest.Id.Value)).Count == 3, "No-op update must not create a duplicate outbox message.");

// Declined.
var declined = NewRequest("decline.afwal", t0.AddMinutes(10));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).AddAsync(declined);
declined.Decline(t0.AddMinutes(11));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).UpdateAsync(declined);
Assert((await EventsAsync(declined.Id.Value)).Any(x => x.EventType == PaymentRequestOutboxEventTypes.Declined),
    "Declined must produce an outbox message.");

// Cancelled.
var cancelled = NewRequest("cancel.afwal", t0.AddMinutes(20));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).AddAsync(cancelled);
cancelled.Cancel(t0.AddMinutes(21));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).UpdateAsync(cancelled);
Assert((await EventsAsync(cancelled.Id.Value)).Any(x => x.EventType == PaymentRequestOutboxEventTypes.Cancelled),
    "Cancelled must produce an outbox message.");

// Expired.
var expired = NewRequest("expire.afwal", t0.AddMinutes(30), t0.AddMinutes(31));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).AddAsync(expired);
expired.Expire(t0.AddMinutes(31));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).UpdateAsync(expired);
Assert((await EventsAsync(expired.Id.Value)).Any(x => x.EventType == PaymentRequestOutboxEventTypes.Expired),
    "Expired must produce an outbox message.");

// Raw QR values are never copied into event payloads.
var qr = PaymentRequest.Create(
    requester,
    RecipientReference.FromQrToken("opaque-secret-qr-token"),
    Currency.Create("EUR"),
    500,
    Guid.NewGuid(),
    t0.AddMinutes(40));
await using (var db = new PaymentRequestDbContext(options)) await new EfPaymentRequestRepository(db).AddAsync(qr);
Assert((await EventsAsync(qr.Id.Value)).All(x => !x.PayloadJson.Contains("opaque-secret-qr-token", StringComparison.Ordinal)),
    "Outbox payload must not contain raw QR token.");

// Transactionality: force an outbox uniqueness failure and ensure the PaymentRequest insert rolls back.
var rollback = NewRequest("rollback.afwal", t0.AddMinutes(50));
await using (var db = new PaymentRequestDbContext(options))
{
    db.PaymentRequestIntegrationOutbox.Add(new PaymentRequestOutboxMessage
    {
        Id = Guid.NewGuid(),
        PaymentRequestId = rollback.Id.Value,
        EventType = PaymentRequestOutboxEventTypes.Created,
        PayloadJson = "{}",
        OccurredAtUtc = rollback.CreatedAtUtc.ToString("O")
    });
    await db.SaveChangesAsync();
}

await using (var db = new PaymentRequestDbContext(options))
{
    var repository = new EfPaymentRequestRepository(db);
    await AssertThrowsAsync<InvalidOperationException>(
        () => repository.AddAsync(rollback),
        "Outbox failure must fail the aggregate write.");
}

await using (var db = new PaymentRequestDbContext(options))
{
    var aggregateExists = await db.PaymentRequests.AsNoTracking().AnyAsync(x => x.Id == rollback.Id.Value);
    Assert(!aggregateExists, "PaymentRequest insert must roll back when outbox insert fails.");
}

Console.WriteLine("AFW-BE-REQUEST transactional event production and outbox scenarios: PASS");
