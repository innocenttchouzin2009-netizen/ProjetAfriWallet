using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-notification-hardening-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath};Default Timeout=5";

try
{
    var options = new DbContextOptionsBuilder<NotificationInboxDbContext>()
        .UseSqlite(connectionString)
        .Options;

    await using (var setup = new NotificationInboxDbContext(options))
        await setup.Database.EnsureCreatedAsync();

    var userId = Guid.NewGuid();
    var sharedTime = new DateTimeOffset(2026, 9, 15, 18, 0, 0, TimeSpan.Zero);
    var notifications = Enumerable.Range(0, 5)
        .Select(_ => InAppNotification.New(userId, PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Created, sharedTime)))
        .ToArray();

    await using (var writeDb = new NotificationInboxDbContext(options))
    {
        var repository = new EfInAppNotificationRepository(writeDb);
        foreach (var notification in notifications)
            Assert(await repository.AddAsync(notification), "Initial notification insert must succeed.");
    }

    await using (var pageDb = new NotificationInboxDbContext(options))
    {
        var service = new InAppNotificationInboxService(new EfInAppNotificationRepository(pageDb));
        var seen = new HashSet<Guid>();
        string? cursor = null;
        do
        {
            var page = await service.ListAsync(userId, false, 2, cursor);
            foreach (var item in page.Items)
                Assert(seen.Add(item.Id), "Stable cursor pagination must not repeat notifications.");
            cursor = page.NextCursor;
        } while (cursor is not null);
        Assert(seen.Count == notifications.Length, "Stable cursor pagination must return every notification exactly once.");

        try
        {
            await service.ListAsync(userId, false, 2, "broken-cursor");
            throw new InvalidOperationException("Malformed cursor should have failed.");
        }
        catch (ArgumentException) { }
    }

    var oldUser = Guid.NewGuid();
    var oldNotification = InAppNotification.New(
        oldUser,
        PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Created, sharedTime.AddDays(-120)));
    var recentNotification = InAppNotification.New(
        oldUser,
        PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Created, sharedTime.AddDays(-5)));

    await using (var retentionDb = new NotificationInboxDbContext(options))
    {
        var repository = new EfInAppNotificationRepository(retentionDb);
        await repository.AddAsync(oldNotification);
        await repository.AddAsync(recentNotification);
        var retention = new NotificationRetentionService(repository, new NotificationRetentionOptions(TimeSpan.FromDays(90)));
        var archived = await retention.ArchiveExpiredAsync(sharedTime);
        Assert(archived == 1, "Retention must archive exactly the expired active notification.");
        Assert(await repository.GetAsync(oldUser, oldNotification.Id) is null, "Archived notification must disappear from active detail.");
        Assert(await repository.GetAsync(oldUser, recentNotification.Id) is not null, "Recent notification must remain active.");
        Assert(await repository.CountUnreadAsync(oldUser) == 1, "Archived unread notification must not count as active unread.");
    }

    await using (var verifyDb = new NotificationInboxDbContext(options))
    {
        var archivedEntity = await verifyDb.Notifications.AsNoTracking().SingleAsync(x => x.Id == oldNotification.Id);
        Assert(archivedEntity.ArchivedAtUtc is not null, "Retention must preserve the archived row and timestamp it.");
    }

    var replayUser = Guid.NewGuid();
    var replayEvent = PaymentRequestEvent.New(Guid.NewGuid(), PaymentRequestEventKind.Accepted, sharedTime.AddMinutes(1));
    var tasks = Enumerable.Range(0, 8).Select(async _ =>
    {
        await using var concurrentDb = new NotificationInboxDbContext(options);
        var repository = new EfInAppNotificationRepository(concurrentDb);
        return await repository.AddAsync(InAppNotification.New(replayUser, replayEvent));
    }).ToArray();

    var results = await Task.WhenAll(tasks);
    Assert(results.Count(x => x) == 1, "Concurrent replay must report exactly one successful delivery.");

    await using (var verifyReplayDb = new NotificationInboxDbContext(options))
    {
        var rows = await verifyReplayDb.Notifications.AsNoTracking()
            .CountAsync(x => x.UserId == replayUser && x.EventId == replayEvent.EventId);
        Assert(rows == 1, "Concurrent replay must persist exactly one notification row.");
    }

    Console.WriteLine("AFW-BE-NOTIFICATION-1 inbox pagination, retention and delivery hardening scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
