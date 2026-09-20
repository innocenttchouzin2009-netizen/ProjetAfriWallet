using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

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

var requestId = Guid.NewGuid();
var occurredAt = new DateTimeOffset(2026, 9, 14, 19, 0, 0, TimeSpan.Zero);

foreach (var kind in new[]
{
    PaymentRequestEventKind.Created,
    PaymentRequestEventKind.Accepted,
    PaymentRequestEventKind.Declined,
    PaymentRequestEventKind.Cancelled,
    PaymentRequestEventKind.Expired
})
{
    var value = PaymentRequestEvent.New(requestId, kind, occurredAt);
    Assert(value.EventId != Guid.Empty, $"{kind} event id must be generated.");
    Assert(value.PaymentRequestId == requestId, $"{kind} request id must be preserved.");
    Assert(value.Kind == kind, $"{kind} kind must be preserved.");
    Assert(value.OccurredAtUtc == occurredAt, $"{kind} timestamp must be preserved.");
    Assert(value.TransferId is null, $"{kind} must not carry transfer id.");
}

var transferId = Guid.NewGuid();
var paid = PaymentRequestEvent.New(requestId, PaymentRequestEventKind.Paid, occurredAt, transferId);
Assert(paid.TransferId == transferId, "Paid event must carry transfer id.");

AssertThrows<ArgumentException>(
    () => PaymentRequestEvent.Create(Guid.Empty, requestId, PaymentRequestEventKind.Created, occurredAt),
    "Empty event id must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequestEvent.New(Guid.Empty, PaymentRequestEventKind.Created, occurredAt),
    "Empty payment request id must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequestEvent.New(requestId, PaymentRequestEventKind.Created, occurredAt.ToOffset(TimeSpan.FromHours(1))),
    "Non-UTC timestamp must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequestEvent.New(requestId, PaymentRequestEventKind.Paid, occurredAt),
    "Paid event without transfer id must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequestEvent.New(requestId, PaymentRequestEventKind.Created, occurredAt, transferId),
    "Non-paid event with transfer id must be rejected.");

var publisher = new RecordingPublisher();
var dispatcher = new PaymentRequestEventDispatcher(publisher);
var created = PaymentRequestEvent.New(requestId, PaymentRequestEventKind.Created, occurredAt);
await dispatcher.PublishAsync(created);
Assert(publisher.Events.Count == 1 && publisher.Events[0] == created, "Dispatcher must forward event unchanged.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await dispatcher.PublishAsync(created, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }
Assert(publisher.Events.Count == 1, "Cancelled dispatch must not publish.");

Console.WriteLine("AFW-BE-NOTIFICATION-1 payment request event foundation scenarios: PASS");

sealed class RecordingPublisher : IPaymentRequestEventPublisher
{
    public List<PaymentRequestEvent> Events { get; } = [];

    public Task PublishAsync(PaymentRequestEvent paymentRequestEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Events.Add(paymentRequestEvent);
        return Task.CompletedTask;
    }
}
