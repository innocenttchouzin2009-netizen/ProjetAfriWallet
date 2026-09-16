using AfriWallet.Notifications.Application;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 16, 11, 0, 0, TimeSpan.Zero);
var eventId = Guid.NewGuid();
var recipientUserId = Guid.NewGuid();
var message = PushNotificationMessage.Create(
    eventId,
    recipientUserId,
    "  Payment received  ",
    "  Your payment request was paid.  ",
    "  afwal://payment-requests/123  ",
    now);

Assert(message.EventId == eventId, "Event id must be preserved.");
Assert(message.RecipientUserId == recipientUserId, "Recipient user id must be preserved.");
Assert(message.Title == "Payment received", "Title must be trimmed.");
Assert(message.Body == "Your payment request was paid.", "Body must be trimmed.");
Assert(message.DeepLink == "afwal://payment-requests/123", "Deep link must be trimmed.");
Assert(message.CreatedAtUtc == now, "Timestamp must be preserved.");

AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(Guid.Empty, recipientUserId, "T", "B", null, now),
    "Empty event id must be rejected.");
AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(eventId, Guid.Empty, "T", "B", null, now),
    "Empty recipient id must be rejected.");
AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(eventId, recipientUserId, " ", "B", null, now),
    "Blank title must be rejected.");
AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(eventId, recipientUserId, "T", " ", null, now),
    "Blank body must be rejected.");
AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(eventId, recipientUserId, new string('x', 121), "B", null, now),
    "Oversized title must be rejected.");
AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(eventId, recipientUserId, "T", new string('x', 513), null, now),
    "Oversized body must be rejected.");
AssertThrows<ArgumentException>(
    () => PushNotificationMessage.Create(eventId, recipientUserId, "T", "B", null, now.ToOffset(TimeSpan.FromHours(2))),
    "Non-UTC timestamp must be rejected.");

var delivered = PushDeliveryResult.Delivered(" provider-123 ");
Assert(delivered.Disposition == PushDeliveryDisposition.Delivered, "Delivered result expected.");
Assert(delivered.ProviderMessageId == "provider-123", "Provider id must be normalized.");
Assert(delivered.ErrorCode is null && delivered.RetryAfter is null, "Delivered result must not carry failure data.");

var retryable = PushDeliveryResult.Retryable(" provider-unavailable ", TimeSpan.FromSeconds(30));
Assert(retryable.Disposition == PushDeliveryDisposition.RetryableFailure, "Retryable result expected.");
Assert(retryable.ErrorCode == "provider-unavailable", "Retry error code must be normalized.");
Assert(retryable.RetryAfter == TimeSpan.FromSeconds(30), "Retry delay must be preserved.");

var permanent = PushDeliveryResult.Permanent(" invalid-target ");
Assert(permanent.Disposition == PushDeliveryDisposition.PermanentFailure, "Permanent result expected.");
Assert(permanent.ErrorCode == "invalid-target", "Permanent error code must be normalized.");
AssertThrows<ArgumentOutOfRangeException>(
    () => PushDeliveryResult.Retryable("provider-unavailable", TimeSpan.FromSeconds(-1)),
    "Negative retry delay must be rejected.");

var transport = new RecordingTransport(PushDeliveryResult.Delivered("message-1"));
var service = new PushNotificationDeliveryService(transport);
var result = await service.DeliverAsync(message);
Assert(result.Disposition == PushDeliveryDisposition.Delivered, "Delivery result must be returned unchanged.");
Assert(transport.Calls == 1, "Transport must be called once.");
Assert(ReferenceEquals(transport.LastMessage, message), "Message must be forwarded unchanged.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.DeliverAsync(message, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }
Assert(transport.Calls == 1, "Cancelled delivery must not call transport.");

Console.WriteLine("AFW-BE-PUSH-NOTIFICATION-1 provider-neutral push delivery foundation scenarios: PASS");

sealed class RecordingTransport(PushDeliveryResult result) : IPushNotificationTransport
{
    public int Calls { get; private set; }
    public PushNotificationMessage? LastMessage { get; private set; }

    public Task<PushDeliveryResult> SendAsync(
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastMessage = message;
        return Task.FromResult(result);
    }
}
