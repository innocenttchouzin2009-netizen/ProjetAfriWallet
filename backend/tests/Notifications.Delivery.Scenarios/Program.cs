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

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-notification-deliveries-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<NotificationDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

try
{
    await using (var setup = new NotificationDbContext(options))
    {
        await setup.Database.EnsureCreatedAsync();
    }

    var eventId = Guid.NewGuid();
    var recipient = Guid.NewGuid();
    var paymentRequestId = Guid.NewGuid();
    var createdAt = new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.Zero);
    var dispatchAt = createdAt.AddSeconds(1);

    var inAppNotification = new InAppNotification(
        eventId,
        eventId,
        recipient,
        paymentRequestId,
        "payment-request.created",
        5_000,
        "EUR",
        "Payment request received",
        "A new payment request requires your attention.",
        createdAt,
        NotificationChannel.InApp);

    await using (var db = new NotificationDbContext(options))
    {
        var repository = new EfNotificationDeliveryRepository(db);
        var dispatcher = new RecordingDispatchPort(NotificationChannel.InApp);
        var fanout = new NotificationDeliveryFanoutService(
            repository,
            [dispatcher],
            new FixedTimeProvider(dispatchAt));
        var service = new PersistentNotificationDeliveryService(fanout);

        await service.DeliverAsync(inAppNotification);
        await service.DeliverAsync(inAppNotification);

        Assert(await db.NotificationDeliveries.CountAsync() == 1,
            "Identical EventId + Channel replay must persist exactly one delivery.");
        Assert(dispatcher.Calls == 1,
            "Already dispatched delivery must not be dispatched twice.");

        var stored = await repository.GetAsync(eventId, NotificationChannel.InApp);
        Assert(stored is not null, "Persisted delivery must be readable.");
        Assert(stored!.Status == NotificationDeliveryStatus.Dispatched,
            "Successful dispatch must mark delivery dispatched.");
        Assert(stored.DispatchedAtUtc == dispatchAt,
            "Dispatch timestamp must be persisted.");
    }

    await using (var db = new NotificationDbContext(options))
    {
        var repository = new EfNotificationDeliveryRepository(db);
        var conflicting = NotificationDelivery.From(inAppNotification with { Body = "Different content" });

        await AssertThrowsAsync<InvalidOperationException>(
            () => repository.GetOrAddAsync(conflicting),
            "Same EventId + Channel with different content must fail closed.");

        var push = NotificationDelivery.From(inAppNotification, NotificationChannel.Push);

        var persistedPush = await repository.GetOrAddAsync(push);
        Assert(persistedPush.Channel == NotificationChannel.Push,
            "Same EventId on a different channel must be allowed.");
        Assert(await db.NotificationDeliveries.CountAsync() == 2,
            "Composite idempotency key must be EventId + Channel.");
    }

    var fanoutEventId = Guid.NewGuid();
    var fanoutNotification = inAppNotification with
    {
        NotificationId = fanoutEventId,
        SourceEventId = fanoutEventId
    };

    await using (var db = new NotificationDbContext(options))
    {
        var repository = new EfNotificationDeliveryRepository(db);
        var inAppDispatcher = new RecordingDispatchPort(NotificationChannel.InApp);
        var pushDispatcher = new RecordingDispatchPort(NotificationChannel.Push);
        var fanout = new NotificationDeliveryFanoutService(
            repository,
            [inAppDispatcher, pushDispatcher],
            new FixedTimeProvider(dispatchAt.AddMinutes(1)));

        await fanout.DispatchAsync(
            fanoutNotification,
            new[] { NotificationChannel.InApp, NotificationChannel.Push });

        await fanout.DispatchAsync(
            fanoutNotification,
            new[] { NotificationChannel.InApp, NotificationChannel.Push });

        Assert(inAppDispatcher.Calls == 1, "In-App fan-out must dispatch exactly once.");
        Assert(pushDispatcher.Calls == 1, "Push fan-out must dispatch exactly once.");

        var storedInApp = await repository.GetAsync(fanoutEventId, NotificationChannel.InApp);
        var storedPush = await repository.GetAsync(fanoutEventId, NotificationChannel.Push);
        Assert(storedInApp?.Status == NotificationDeliveryStatus.Dispatched,
            "In-App fan-out delivery must be persisted and dispatched.");
        Assert(storedPush?.Status == NotificationDeliveryStatus.Dispatched,
            "Push fan-out delivery must be persisted and dispatched.");
    }

    var failClosedEventId = Guid.NewGuid();
    var failClosedNotification = inAppNotification with
    {
        NotificationId = failClosedEventId,
        SourceEventId = failClosedEventId
    };

    await using (var db = new NotificationDbContext(options))
    {
        var repository = new EfNotificationDeliveryRepository(db);
        var inAppDispatcher = new RecordingDispatchPort(NotificationChannel.InApp);
        var fanout = new NotificationDeliveryFanoutService(
            repository,
            [inAppDispatcher],
            new FixedTimeProvider(dispatchAt.AddMinutes(2)));

        await AssertThrowsAsync<InvalidOperationException>(
            () => fanout.DispatchAsync(
                failClosedNotification,
                new[] { NotificationChannel.InApp, NotificationChannel.Push }),
            "Missing Push dispatcher must fail closed before any delivery is persisted.");

        Assert(inAppDispatcher.Calls == 0,
            "Fail-closed validation must happen before In-App dispatch.");
        Assert(await repository.GetAsync(failClosedEventId, NotificationChannel.InApp) is null,
            "Missing dispatcher must not leave a partial In-App delivery.");
        Assert(await repository.GetAsync(failClosedEventId, NotificationChannel.Push) is null,
            "Missing dispatcher must not persist a Push delivery.");
    }

    await using (var db = new NotificationDbContext(options))
    {
        var repository = new EfNotificationDeliveryRepository(db);
        var duplicateA = new RecordingDispatchPort(NotificationChannel.InApp);
        var duplicateB = new RecordingDispatchPort(NotificationChannel.InApp);
        var fanout = new NotificationDeliveryFanoutService(
            repository,
            [duplicateA, duplicateB],
            new FixedTimeProvider(dispatchAt.AddMinutes(3)));

        await AssertThrowsAsync<InvalidOperationException>(
            () => fanout.DispatchAsync(
                failClosedNotification,
                new[] { NotificationChannel.InApp }),
            "Duplicate dispatchers for one channel must fail closed.");
    }

    using (var cts = new CancellationTokenSource())
    {
        cts.Cancel();
        await using var db = new NotificationDbContext(options);
        var repository = new EfNotificationDeliveryRepository(db);
        await AssertThrowsAsync<OperationCanceledException>(
            () => repository.GetAsync(Guid.NewGuid(), NotificationChannel.InApp, cts.Token),
            "Delivery repository must propagate cancellation.");
    }

    Console.WriteLine("AFW-BE-NOTIFICATION-DELIVERY-1 channel-aware durable delivery scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class RecordingDispatchPort(NotificationChannel channel) : INotificationDispatchPort
{
    public NotificationChannel Channel => channel;
    public int Calls { get; private set; }

    public Task DispatchAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (delivery.Channel != Channel)
            throw new InvalidOperationException("Dispatcher received the wrong channel.");
        Calls++;
        return Task.CompletedTask;
    }
}

sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
