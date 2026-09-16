using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Notification.Persistence;
using AfriWallet.Wallet.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<PaymentRequestNotificationDbContext>()
    .UseSqlite(connection)
    .Options;

await using (var setup = new PaymentRequestNotificationDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

var sourceEventId = Guid.NewGuid();
var requestId = PaymentRequestId.From(Guid.NewGuid());
var recipientOwnerId = Guid.NewGuid();
var requesterWalletId = WalletId.From(Guid.NewGuid());
var occurredAt = new DateTimeOffset(2026, 9, 16, 6, 0, 0, TimeSpan.Zero);

var notification = PaymentRequestNotification.Project(
    sourceEventId,
    requestId,
    PaymentRequestNotificationKind.Created,
    PaymentRequestNotificationAudience.Requester,
    recipientOwnerId,
    requesterWalletId,
    Currency.Create("XAF"),
    12_500,
    occurredAt,
    PaymentRequestStatus.Pending,
    occurredAt.AddDays(1));

await using (var db = new PaymentRequestNotificationDbContext(options))
{
    var store = new EfPaymentRequestNotificationProjectionStore(db);
    Assert(await store.TryAddAsync(notification), "First projection must be persisted.");

    var duplicate = PaymentRequestNotification.Project(
        sourceEventId,
        requestId,
        PaymentRequestNotificationKind.Created,
        PaymentRequestNotificationAudience.Requester,
        recipientOwnerId,
        requesterWalletId,
        Currency.Create("XAF"),
        12_500,
        occurredAt,
        PaymentRequestStatus.Pending,
        occurredAt.AddDays(1));

    Assert(!await store.TryAddAsync(duplicate), "Same source event, recipient and audience must be idempotent.");

    var payerAudience = PaymentRequestNotification.Project(
        sourceEventId,
        requestId,
        PaymentRequestNotificationKind.Created,
        PaymentRequestNotificationAudience.Payer,
        recipientOwnerId,
        requesterWalletId,
        Currency.Create("XAF"),
        12_500,
        occurredAt,
        PaymentRequestStatus.Pending,
        occurredAt.AddDays(1));

    Assert(await store.TryAddAsync(payerAudience), "Different audience must have an independent projection key.");
}

await using (var readDb = new PaymentRequestNotificationDbContext(options))
{
    var store = new EfPaymentRequestNotificationProjectionStore(readDb);
    var restored = await store.GetAsync(notification.Id);
    Assert(restored is not null, "Persisted notification must be readable.");
    Assert(restored!.ReadStatus == PaymentRequestNotificationReadStatus.Unread, "New projection must remain unread after round-trip.");
    Assert(restored.ReadAtUtc is null, "Unread projection must not have a read timestamp.");
    Assert(restored.SourceEventId == sourceEventId, "Source event id must round-trip.");
    Assert(restored.RecipientOwnerId == recipientOwnerId, "Recipient owner id must round-trip.");
    Assert(restored.Audience == PaymentRequestNotificationAudience.Requester, "Audience must round-trip.");

    var readAt = occurredAt.AddMinutes(5);
    restored.MarkRead(readAt);
    await store.UpdateAsync(restored);
}

await using (var verifyDb = new PaymentRequestNotificationDbContext(options))
{
    var store = new EfPaymentRequestNotificationProjectionStore(verifyDb);
    var restored = await store.GetAsync(notification.Id);
    Assert(restored is not null, "Updated notification must remain readable.");
    Assert(restored!.ReadStatus == PaymentRequestNotificationReadStatus.Read, "Read state must persist.");
    Assert(restored.ReadAtUtc == occurredAt.AddMinutes(5), "Read timestamp must persist.");

    var count = await verifyDb.Notifications.CountAsync();
    Assert(count == 2, "Duplicate projection must not create a second requester row.");
}

using (var cts = new CancellationTokenSource())
{
    cts.Cancel();
    await using var db = new PaymentRequestNotificationDbContext(options);
    var store = new EfPaymentRequestNotificationProjectionStore(db);
    try
    {
        await store.GetAsync(notification.Id, cts.Token);
        throw new InvalidOperationException("Expected cancellation.");
    }
    catch (OperationCanceledException) { }
}

Console.WriteLine("AFW-BE-REQUEST-NOTIFY-1 durable notification projection persistence scenarios: PASS");
