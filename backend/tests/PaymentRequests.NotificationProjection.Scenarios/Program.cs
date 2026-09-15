using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
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

var occurredAt = new DateTimeOffset(2026, 9, 16, 0, 30, 0, TimeSpan.Zero);
var requestId = PaymentRequestId.From(Guid.NewGuid());
var requesterWalletId = WalletId.From(Guid.NewGuid());
var recipientOwnerId = Guid.NewGuid();
var sourceEventId = Guid.NewGuid();
var currency = Currency.Create("XAF");

var created = PaymentRequestNotification.Project(
    sourceEventId,
    requestId,
    PaymentRequestNotificationKind.Created,
    PaymentRequestNotificationAudience.Payer,
    recipientOwnerId,
    requesterWalletId,
    currency,
    25_000,
    occurredAt,
    PaymentRequestStatus.Pending,
    occurredAt.AddDays(1));

Assert(created.Id.Value != Guid.Empty, "Notification id must be generated.");
Assert(created.SourceEventId == sourceEventId, "Source event id must be preserved.");
Assert(created.PaymentRequestId == requestId, "Payment request id must be preserved.");
Assert(created.Kind == PaymentRequestNotificationKind.Created, "Notification kind mismatch.");
Assert(created.Audience == PaymentRequestNotificationAudience.Payer, "Audience mismatch.");
Assert(created.RecipientOwnerId == recipientOwnerId, "Recipient owner mismatch.");
Assert(created.Currency.Code == "XAF", "Currency must remain normalized.");
Assert(created.AmountMinor == 25_000, "Amount mismatch.");
Assert(created.ReadStatus == PaymentRequestNotificationReadStatus.Unread, "New notification must be unread.");
Assert(created.ReadAtUtc is null, "Unread notification cannot have read timestamp.");

created.MarkRead(occurredAt.AddMinutes(5));
Assert(created.ReadStatus == PaymentRequestNotificationReadStatus.Read, "Notification must become read.");
Assert(created.ReadAtUtc == occurredAt.AddMinutes(5), "Read timestamp must be recorded.");
created.MarkRead(occurredAt.AddMinutes(6));
Assert(created.ReadAtUtc == occurredAt.AddMinutes(5), "Idempotent read must preserve first read timestamp.");
AssertThrows<ArgumentException>(
    () => created.MarkRead(occurredAt.AddMinutes(4)),
    "Read timestamp cannot move backwards.");

var transferId = Guid.NewGuid();
var paid = PaymentRequestNotification.Project(
    Guid.NewGuid(),
    requestId,
    PaymentRequestNotificationKind.Paid,
    PaymentRequestNotificationAudience.Requester,
    Guid.NewGuid(),
    requesterWalletId,
    Currency.Create("EUR"),
    5_000,
    occurredAt.AddMinutes(10),
    PaymentRequestStatus.Paid,
    transferId: transferId);
Assert(paid.TransferId == transferId, "Paid notification must retain transfer id.");

AssertThrows<ArgumentException>(
    () => PaymentRequestNotification.Project(
        Guid.NewGuid(), requestId, PaymentRequestNotificationKind.Paid,
        PaymentRequestNotificationAudience.Requester, Guid.NewGuid(), requesterWalletId,
        currency, 1_000, occurredAt, PaymentRequestStatus.Paid),
    "Paid notification must require transfer id.");

AssertThrows<ArgumentException>(
    () => PaymentRequestNotification.Project(
        Guid.NewGuid(), requestId, PaymentRequestNotificationKind.Accepted,
        PaymentRequestNotificationAudience.Requester, Guid.NewGuid(), requesterWalletId,
        currency, 1_000, occurredAt, PaymentRequestStatus.Accepted, transferId: Guid.NewGuid()),
    "Non-paid notification must reject transfer id.");

AssertThrows<InvalidOperationException>(
    () => PaymentRequestNotification.Project(
        Guid.NewGuid(), requestId, PaymentRequestNotificationKind.Declined,
        PaymentRequestNotificationAudience.Requester, Guid.NewGuid(), requesterWalletId,
        currency, 1_000, occurredAt, PaymentRequestStatus.Pending),
    "Notification kind and request status must agree.");

AssertThrows<ArgumentException>(
    () => PaymentRequestNotification.Project(
        Guid.Empty, requestId, PaymentRequestNotificationKind.Created,
        PaymentRequestNotificationAudience.Payer, Guid.NewGuid(), requesterWalletId,
        currency, 1_000, occurredAt, PaymentRequestStatus.Pending),
    "Source event id must be required.");

AssertThrows<ArgumentException>(
    () => PaymentRequestNotification.Project(
        Guid.NewGuid(), requestId, PaymentRequestNotificationKind.Created,
        PaymentRequestNotificationAudience.Payer, Guid.Empty, requesterWalletId,
        currency, 1_000, occurredAt, PaymentRequestStatus.Pending),
    "Recipient owner id must be required.");

AssertThrows<ArgumentException>(
    () => PaymentRequestNotification.Project(
        Guid.NewGuid(), requestId, PaymentRequestNotificationKind.Created,
        PaymentRequestNotificationAudience.Payer, Guid.NewGuid(), requesterWalletId,
        currency, 1_000, occurredAt.ToOffset(TimeSpan.FromHours(1)), PaymentRequestStatus.Pending),
    "Notification timestamp must be UTC.");

var expectedMappings = new Dictionary<string, PaymentRequestNotificationKind>
{
    [PaymentRequestNotificationEventTypes.Created] = PaymentRequestNotificationKind.Created,
    [PaymentRequestNotificationEventTypes.Accepted] = PaymentRequestNotificationKind.Accepted,
    [PaymentRequestNotificationEventTypes.Declined] = PaymentRequestNotificationKind.Declined,
    [PaymentRequestNotificationEventTypes.Cancelled] = PaymentRequestNotificationKind.Cancelled,
    [PaymentRequestNotificationEventTypes.Expired] = PaymentRequestNotificationKind.Expired,
    [PaymentRequestNotificationEventTypes.Paid] = PaymentRequestNotificationKind.Paid
};

foreach (var pair in expectedMappings)
{
    Assert(PaymentRequestNotificationEventTypes.TryGetKind(pair.Key, out var kind), $"{pair.Key} must be supported.");
    Assert(kind == pair.Value, $"{pair.Key} mapping mismatch.");
}

Assert(!PaymentRequestNotificationEventTypes.TryGetKind("payment-request.unknown", out _), "Unknown event types must be ignored by projection contracts.");

var projectionSource = new PaymentRequestNotificationProjectionSource(
    sourceEventId,
    requestId,
    PaymentRequestNotificationEventTypes.Created,
    occurredAt,
    PaymentRequestStatus.Pending,
    requesterWalletId,
    currency,
    25_000,
    occurredAt.AddDays(1),
    null,
    null);
var recipient = new PaymentRequestNotificationRecipient(recipientOwnerId, PaymentRequestNotificationAudience.Payer);
var key = new PaymentRequestNotificationProjectionKey(sourceEventId, recipient.OwnerId, recipient.Audience);
Assert(projectionSource.PaymentRequestId == requestId, "Projection source must preserve request id.");
Assert(key.SourceEventId == sourceEventId && key.RecipientOwnerId == recipientOwnerId, "Projection key must bind source event and recipient.");

Console.WriteLine("AFW-BE-REQUEST-NOTIFY-1 notification domain and projection contract scenarios: PASS");
