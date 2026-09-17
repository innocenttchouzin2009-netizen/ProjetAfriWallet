using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using IdentityService.Api.Notifications;
using Microsoft.Extensions.DependencyInjection;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var services = new ServiceCollection();
services.AddInAppNotifications("Data Source=:memory:");
services.AddNotificationPreferences("Data Source=:memory:");

Assert(services.Any(x => x.ServiceType == typeof(NotificationDeliveryRoutingService)),
    "Runtime composition must register NotificationDeliveryRoutingService.");
Assert(services.Any(x => x.ServiceType == typeof(NotificationDeliveryDispatchService)),
    "Runtime composition must register NotificationDeliveryDispatchService.");
Assert(services.Any(x => x.ServiceType == typeof(NotificationEventPushDeliveryService)),
    "Runtime composition must register NotificationEventPushDeliveryService.");
Assert(services.Any(x => x.ServiceType == typeof(INotificationPushDispatchPort) &&
                         x.ImplementationType == typeof(RuntimeNotificationPushDispatchPort)),
    "Runtime composition must register the provider-aware push dispatch port.");

var order = new List<string>();
var inAppRepository = new RecordingInAppRepository(order);
var pushPort = new RecordingPushPort(order, attemptedTargets: 1);
var dispatcher = new NotificationDeliveryDispatchService(inAppRepository, pushPort);
var now = new DateTimeOffset(2026, 9, 17, 15, 45, 0, TimeSpan.Zero);
var notificationEvent = PaymentRequestEvent.Create(
    Guid.NewGuid(), Guid.NewGuid(), PaymentRequestEventKind.Created, now);
var notification = InAppNotification.New(Guid.NewGuid(), notificationEvent);

var result = await dispatcher.DispatchAsync(notification, now);
Assert(result.InAppAdded, "In-App delivery must be persisted.");
Assert(inAppRepository.AddCalls == 1, "In-App repository must be called exactly once.");
Assert(pushPort.Calls == 1, "Push dispatch port must be evaluated after In-App persistence.");
Assert(order.SequenceEqual(["in-app", "push"]), "In-App must be persisted before Push evaluation.");

var suppressedOrder = new List<string>();
var suppressedInbox = new RecordingInAppRepository(suppressedOrder);
var suppressedPush = new RecordingPushPort(suppressedOrder, attemptedTargets: 0);
var suppressedDispatcher = new NotificationDeliveryDispatchService(suppressedInbox, suppressedPush);
var suppressedNotification = InAppNotification.New(Guid.NewGuid(), notificationEvent);
var suppressedResult = await suppressedDispatcher.DispatchAsync(suppressedNotification, now);

Assert(suppressedInbox.AddCalls == 1, "In-App must still persist when Push is suppressed by routing.");
Assert(suppressedPush.Calls == 1, "Push routing must still be evaluated.");
Assert(suppressedResult.Push.AttemptedTargets == 0, "Suppressed Push must not attempt a provider target.");
Assert(suppressedOrder.SequenceEqual(["in-app", "push"]), "Suppressed Push must not bypass In-App delivery.");

Console.WriteLine("AFW-BE-NOTIFICATION-ROUTING-1 runtime composition and dispatch scenarios: PASS");

sealed class RecordingPushPort(List<string> order, int attemptedTargets) : INotificationPushDispatchPort
{
    public int Calls { get; private set; }

    public Task<PushEventDeliveryResult> DispatchAsync(
        InAppNotification notification,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        order.Add("push");
        return Task.FromResult(new PushEventDeliveryResult(
            notification.EventId,
            attemptedTargets,
            Delivered: attemptedTargets,
            RetryScheduled: 0,
            TerminalFailures: 0));
    }
}

sealed class RecordingInAppRepository(List<string> order) : IInAppNotificationRepository
{
    public int AddCalls { get; private set; }

    public Task<bool> AddAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddCalls++;
        order.Add("in-app");
        return Task.FromResult(true);
    }

    public Task<InAppNotification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<InAppNotification?>(null);

    public Task<IReadOnlyList<InAppNotification>> ListPageAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        NotificationInboxCursor? cursor,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InAppNotification>>([]);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<int> ArchiveBeforeAsync(DateTimeOffset cutoffUtc, DateTimeOffset archivedAtUtc, CancellationToken cancellationToken = default) => Task.FromResult(0);
}
