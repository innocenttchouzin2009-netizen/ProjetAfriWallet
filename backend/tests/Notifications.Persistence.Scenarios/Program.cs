using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
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

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-notifications-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<NotificationDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

try
{
    await using (var setup = new NotificationDbContext(options))
    {
        await setup.Database.EnsureCreatedAsync();
    }

    var recipient = Guid.NewGuid();
    var otherRecipient = Guid.NewGuid();
    var paymentRequestId = Guid.NewGuid();
    var occurredAt = new DateTimeOffset(2026, 9, 15, 20, 30, 0, TimeSpan.Zero);
    var eventId = Guid.NewGuid();

    var first = new InAppNotification(
        eventId,
        eventId,
        recipient,
        paymentRequestId,
        "payment-request.created",
        12_500,
        "EUR",
        "Payment request received",
        "A new payment request requires your attention.",
        occurredAt,
        NotificationChannel.InApp);

    await using (var db = new NotificationDbContext(options))
    {
        var store = new EfInAppNotificationStore(db);
        await store.DeliverAsync(first);
        await store.DeliverAsync(first);
        Assert(await db.InAppNotifications.CountAsync() == 1, "Identical EventId replay must create exactly one notification.");
    }

    await using (var db = new NotificationDbContext(options))
    {
        var store = new EfInAppNotificationStore(db);
        var conflicting = first with { Body = "Different content" };
        await AssertThrowsAsync<InvalidOperationException>(
            () => store.DeliverAsync(conflicting),
            "Same EventId with different content must fail closed.");
        Assert(await db.InAppNotifications.CountAsync() == 1, "Conflicting replay must not create another notification.");
    }

    var secondEventId = Guid.NewGuid();
    var second = new InAppNotification(
        secondEventId,
        secondEventId,
        recipient,
        paymentRequestId,
        "payment-request.paid",
        12_500,
        "EUR",
        "Payment received",
        "The payment request was paid successfully.",
        occurredAt.AddMinutes(5),
        NotificationChannel.InApp);

    var otherEventId = Guid.NewGuid();
    var other = new InAppNotification(
        otherEventId,
        otherEventId,
        otherRecipient,
        Guid.NewGuid(),
        "payment-request.created",
        2_000,
        "XAF",
        "Payment request received",
        "Another user's notification.",
        occurredAt.AddMinutes(10),
        NotificationChannel.InApp);

    await using (var db = new NotificationDbContext(options))
    {
        var store = new EfInAppNotificationStore(db);
        await store.DeliverAsync(second);
        await store.DeliverAsync(other);

        var list = await store.ListByRecipientAsync(recipient);
        Assert(list.Count == 2, "Recipient must see only their two notifications.");
        Assert(list[0].NotificationId == secondEventId, "Newest notification must be listed first.");
        Assert(list[1].NotificationId == eventId, "Older notification must be listed second.");
        Assert(list.All(x => x.RecipientUserId == recipient), "List must be isolated by recipient user id.");
        Assert(list.All(x => !x.IsRead && x.ReadAtUtc is null), "New notifications must start unread.");

        var own = await store.GetAsync(recipient, eventId);
        Assert(own is not null && own.SourceEventId == eventId, "Recipient must be able to read their notification.");

        var hidden = await store.GetAsync(otherRecipient, eventId);
        Assert(hidden is null, "Notification must be hidden from another recipient.");

        var limited = await store.ListByRecipientAsync(recipient, 1);
        Assert(limited.Count == 1 && limited[0].NotificationId == secondEventId, "Reader limit must be enforced.");
    }

    using (var cts = new CancellationTokenSource())
    {
        cts.Cancel();
        await using var db = new NotificationDbContext(options);
        var store = new EfInAppNotificationStore(db);
        await AssertThrowsAsync<OperationCanceledException>(
            () => store.ListByRecipientAsync(recipient, cancellationToken: cts.Token),
            "Reader must propagate cancellation.");
    }

    Console.WriteLine("AFW-BE-NOTIFICATION-DELIVERY-1 persistent In-App delivery scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
