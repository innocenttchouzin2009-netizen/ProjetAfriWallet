using AfriWallet.Notifications.Application;

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

var now = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.Zero);
var notification = new NotificationDelivery(
    Guid.NewGuid(),
    Guid.NewGuid(),
    NotificationChannel.InApp,
    "payment-request.paid",
    "Payment received",
    "Your payment request was paid.",
    now,
    "{\"paymentRequestId\":\"opaque\"}");

var provider = new RecordingProvider();
var adapter = new InAppNotificationProviderAdapter(provider);
await adapter.DeliverAsync(notification);
Assert(provider.Calls == 1, "Provider must be called exactly once.");
Assert(provider.LastMessage is not null, "Provider message is required.");
Assert(provider.LastMessage!.NotificationId == notification.NotificationId, "Notification id mismatch.");
Assert(provider.LastMessage.RecipientUserId == notification.RecipientUserId, "Recipient mismatch.");
Assert(provider.LastMessage.Type == notification.Type, "Type mismatch.");
Assert(provider.LastMessage.Title == notification.Title, "Title mismatch.");
Assert(provider.LastMessage.Body == notification.Body, "Body mismatch.");
Assert(provider.LastMessage.CreatedAtUtc == notification.CreatedAtUtc, "Timestamp mismatch.");
Assert(provider.LastMessage.DataJson == notification.DataJson, "Data payload mismatch.");

await AssertThrowsAsync<ArgumentException>(
    () => adapter.DeliverAsync(notification with { NotificationId = Guid.Empty }),
    "Empty notification id must be rejected.");
await AssertThrowsAsync<ArgumentException>(
    () => adapter.DeliverAsync(notification with { RecipientUserId = Guid.Empty }),
    "Empty recipient must be rejected.");
await AssertThrowsAsync<ArgumentException>(
    () => adapter.DeliverAsync(notification with { Type = " " }),
    "Blank type must be rejected.");
await AssertThrowsAsync<ArgumentException>(
    () => adapter.DeliverAsync(notification with { Title = " " }),
    "Blank title must be rejected.");
await AssertThrowsAsync<ArgumentException>(
    () => adapter.DeliverAsync(notification with { Body = " " }),
    "Blank body must be rejected.");
await AssertThrowsAsync<ArgumentException>(
    () => adapter.DeliverAsync(notification with { CreatedAtUtc = now.ToOffset(TimeSpan.FromHours(2)) }),
    "Non-UTC timestamp must be rejected.");

var transientAdapter = new InAppNotificationProviderAdapter(
    new ThrowingProvider(InAppNotificationProviderFailureKind.Transient));
try
{
    await transientAdapter.DeliverAsync(notification);
    throw new InvalidOperationException("Expected transient delivery exception.");
}
catch (NotificationDeliveryException ex)
{
    Assert(ex.FailureKind == NotificationDeliveryFailureKind.Transient, "Transient provider failure must remain transient.");
}

var permanentAdapter = new InAppNotificationProviderAdapter(
    new ThrowingProvider(InAppNotificationProviderFailureKind.Permanent));
try
{
    await permanentAdapter.DeliverAsync(notification);
    throw new InvalidOperationException("Expected permanent delivery exception.");
}
catch (NotificationDeliveryException ex)
{
    Assert(ex.FailureKind == NotificationDeliveryFailureKind.Permanent, "Permanent provider failure must remain permanent.");
}

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => adapter.DeliverAsync(notification, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-NOTIFY-1 notification delivery and in-app provider adapter scenarios: PASS");

sealed class RecordingProvider : IInAppNotificationProvider
{
    public int Calls { get; private set; }
    public InAppNotificationMessage? LastMessage { get; private set; }

    public Task PublishAsync(InAppNotificationMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastMessage = message;
        return Task.CompletedTask;
    }
}

sealed class ThrowingProvider(InAppNotificationProviderFailureKind failureKind) : IInAppNotificationProvider
{
    public Task PublishAsync(InAppNotificationMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InAppNotificationProviderException(failureKind, "provider failure");
    }
}
