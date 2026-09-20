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

var recipient = Guid.NewGuid();
var actor = Guid.NewGuid();
var correlation = Guid.NewGuid();
var occurredAt = new DateTimeOffset(2026, 9, 20, 18, 30, 0, TimeSpan.Zero);
var resource = ActivityResourceReference.Create("payment-request", Guid.NewGuid().ToString());

var activity = ActivityEvent.New(
    recipient,
    actor,
    "payment-request.paid",
    resource,
    occurredAt,
    correlation,
    "{\"amountMinor\":2500,\"currency\":\"XAF\"}");

Assert(activity.EventId != Guid.Empty, "Activity event id must be generated.");
Assert(activity.RecipientUserId == recipient, "Recipient user id mismatch.");
Assert(activity.ActorUserId == actor, "Actor user id mismatch.");
Assert(activity.Type == "payment-request.paid", "Activity type mismatch.");
Assert(activity.Resource == resource, "Activity resource mismatch.");
Assert(activity.CorrelationId == correlation, "Correlation id mismatch.");
Assert(activity.MetadataJson is not null && activity.MetadataJson.Contains("\"currency\":\"XAF\""), "Metadata must be preserved.");

var delivery = NotificationDelivery.Create(
    Guid.NewGuid(),
    activity,
    NotificationChannel.InApp,
    "Payment received",
    "Your payment request was paid.",
    occurredAt.AddSeconds(1));

Assert(delivery.EventId == activity.EventId, "Delivery must preserve source event id.");
Assert(delivery.RecipientUserId == recipient, "Delivery recipient mismatch.");
Assert(delivery.Channel == NotificationChannel.InApp, "Delivery channel mismatch.");
Assert(delivery.Type == activity.Type, "Delivery type mismatch.");
Assert(delivery.ResourceType == resource.Type && delivery.ResourceId == resource.Id, "Delivery resource mismatch.");
Assert(delivery.DataJson == activity.MetadataJson, "Activity metadata must flow to delivery by default.");

var pushDelivery = NotificationDelivery.Create(
    Guid.NewGuid(),
    activity,
    NotificationChannel.Push,
    "Payment received",
    "Your payment request was paid.",
    occurredAt.AddSeconds(2),
    "{\"deepLink\":\"afwal://payment-requests\"}");
Assert(pushDelivery.Channel == NotificationChannel.Push, "Push channel must be supported.");
Assert(pushDelivery.DataJson!.Contains("deepLink"), "Explicit delivery data must override activity metadata.");

AssertThrows<ArgumentException>(
    () => ActivityResourceReference.Create("payment request", "1"),
    "Resource type whitespace must be rejected.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.Create(Guid.Empty, recipient, actor, "payment-request.created", resource, occurredAt),
    "Empty event id must be rejected.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(Guid.Empty, actor, "payment-request.created", resource, occurredAt),
    "Empty recipient must be rejected.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(recipient, Guid.Empty, "payment-request.created", resource, occurredAt),
    "Empty actor must be rejected when supplied.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(recipient, actor, "payment request.created", resource, occurredAt),
    "Activity type whitespace must be rejected.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(recipient, actor, "payment-request.created", resource, occurredAt.ToOffset(TimeSpan.FromHours(2))),
    "Non-UTC activity timestamp must be rejected.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(recipient, actor, "payment-request.created", resource, occurredAt, Guid.Empty),
    "Empty correlation id must be rejected when supplied.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(recipient, actor, "payment-request.created", resource, occurredAt, metadataJson: "not-json"),
    "Malformed metadata JSON must be rejected.");
AssertThrows<ArgumentException>(
    () => ActivityEvent.New(recipient, actor, "payment-request.created", resource, occurredAt, metadataJson: "[]"),
    "Metadata JSON must be an object.");

AssertThrows<ArgumentOutOfRangeException>(
    () => NotificationDelivery.Create(Guid.NewGuid(), activity, (NotificationChannel)999, "Title", "Body", occurredAt),
    "Unknown notification channel must be rejected.");
AssertThrows<ArgumentException>(
    () => NotificationDelivery.Create(Guid.NewGuid(), activity, NotificationChannel.InApp, " ", "Body", occurredAt),
    "Blank title must be rejected.");
AssertThrows<ArgumentException>(
    () => NotificationDelivery.Create(Guid.NewGuid(), activity, NotificationChannel.InApp, "Title", "Body", occurredAt.ToOffset(TimeSpan.FromHours(1))),
    "Non-UTC delivery timestamp must be rejected.");

Console.WriteLine("AFW-BE-NOTIFY-1 activity event domain and notification contract scenarios: PASS");
