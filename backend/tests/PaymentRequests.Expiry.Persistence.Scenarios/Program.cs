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

await using var db = new PaymentRequestDbContext(options);
await db.Database.EnsureCreatedAsync();

var repository = new EfPaymentRequestRepository(db);
var reader = new EfPaymentRequestDueReader(db);
var requester = WalletId.From(Guid.NewGuid());
var payer = WalletId.From(Guid.NewGuid());
var reference = RecipientReference.FromAfWalId("payer.afwal");
var currency = Currency.Create("EUR");
var createdAt = new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);
var asOf = createdAt.AddMinutes(10);

PaymentRequest Create(DateTimeOffset? expiresAtUtc) => PaymentRequest.Create(
    requester,
    reference,
    currency,
    5_000,
    Guid.NewGuid(),
    createdAt,
    expiresAtUtc);

var earliestPending = Create(createdAt.AddMinutes(4));
var acceptedDue = Create(createdAt.AddMinutes(5));
acceptedDue.Accept(payer, createdAt.AddMinutes(1));
var boundaryPending = Create(asOf);
var futurePending = Create(createdAt.AddMinutes(20));
var noExpiry = Create(null);
var declinedDue = Create(createdAt.AddMinutes(3));
declinedDue.Decline(createdAt.AddMinutes(2));

foreach (var request in new[]
{
    earliestPending,
    acceptedDue,
    boundaryPending,
    futurePending,
    noExpiry,
    declinedDue
})
{
    await repository.AddAsync(request);
}

var due = await reader.ListDueAsync(asOf, 10);
Assert(due.Count == 3, "Only Pending/Accepted requests due at or before as-of must be returned.");
Assert(due[0].Id == earliestPending.Id, "Due requests must be ordered by earliest expiration first.");
Assert(due[1].Id == acceptedDue.Id, "Accepted due request must be returned in expiration order.");
Assert(due[2].Id == boundaryPending.Id, "Request expiring exactly at as-of must be included.");
Assert(due.All(request => request.Status is PaymentRequestStatus.Pending or PaymentRequestStatus.Accepted),
    "Terminal requests must never be returned.");
Assert(due.All(request => request.ExpiresAtUtc is not null && request.ExpiresAtUtc <= asOf),
    "Reader must return only actually due requests.");

var bounded = await reader.ListDueAsync(asOf, 2);
Assert(bounded.Count == 2, "Reader must apply the requested SQL limit.");
Assert(bounded[0].Id == earliestPending.Id && bounded[1].Id == acceptedDue.Id,
    "Bounded query must preserve deterministic due ordering.");

var beforeAnyDue = await reader.ListDueAsync(createdAt.AddMinutes(1), 10);
Assert(beforeAnyDue.Count == 0, "No request should be returned before its expiration.");

await AssertThrowsAsync<ArgumentException>(
    () => reader.ListDueAsync(asOf.ToOffset(TimeSpan.FromHours(2)), 10),
    "Non-UTC as-of timestamp must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => reader.ListDueAsync(asOf, 0),
    "Zero limit must be rejected.");

await AssertThrowsAsync<ArgumentOutOfRangeException>(
    () => reader.ListDueAsync(asOf, 501),
    "Oversized limit must be rejected.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => reader.ListDueAsync(asOf, 10, cts.Token),
    "Cancellation must propagate before SQLite query execution.");

Console.WriteLine("AFW-BE-REQUEST-EXPIRY-1 SQLite due-request reader scenarios: PASS");
