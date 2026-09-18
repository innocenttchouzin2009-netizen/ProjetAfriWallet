using AfriWallet.Webhooks.Domain;

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

var now = new DateTimeOffset(2026, 9, 18, 20, 0, 0, TimeSpan.Zero);
var ownerId = Guid.NewGuid();

var subscription = WebhookSubscription.Create(
    ownerId,
    new Uri("https://merchant.example/webhooks/afwal"),
    [
        WebhookEventType.Create("payment-request.PAID"),
        WebhookEventType.Create("payment-request.created"),
        WebhookEventType.Create("payment-request.created")
    ],
    "secrets/merchant-42/webhook-signing",
    now);

Assert(subscription.Id.Value != Guid.Empty, "Subscription id must be generated.");
Assert(subscription.OwnerId == ownerId, "Owner must be preserved.");
Assert(subscription.Status == WebhookSubscriptionStatus.Active, "New subscription must be active.");
Assert(subscription.EventTypes.Count == 2, "Event types must be deduplicated.");
Assert(subscription.EventTypes[0].Value == "payment-request.created", "Event type normalization must be deterministic.");
Assert(subscription.EventTypes[1].Value == "payment-request.paid", "Event type normalization must lowercase values.");
Assert(subscription.SigningKeyReference == "secrets/merchant-42/webhook-signing", "Only the signing key reference is stored.");

subscription.Disable(now.AddMinutes(1));
Assert(subscription.Status == WebhookSubscriptionStatus.Disabled, "Subscription must be disabled.");
subscription.Enable(now.AddMinutes(2));
Assert(subscription.Status == WebhookSubscriptionStatus.Active, "Subscription must be re-enabled.");
subscription.ChangeEndpoint(new Uri("https://merchant.example/hooks/afwal-v2"), now.AddMinutes(3));
Assert(subscription.Endpoint.AbsoluteUri == "https://merchant.example/hooks/afwal-v2", "Endpoint must be replaceable.");
subscription.ReplaceEventTypes([WebhookEventType.Create("payment-request.accepted")], now.AddMinutes(4));
Assert(subscription.EventTypes.Single().Value == "payment-request.accepted", "Event filter must be replaceable.");
subscription.ChangeSigningKeyReference("vault/merchant-42/key-v2", now.AddMinutes(5));
Assert(subscription.SigningKeyReference == "vault/merchant-42/key-v2", "Signing key reference must be rotatable.");

AssertThrows<ArgumentException>(
    () => WebhookSubscription.Create(
        ownerId,
        new Uri("http://merchant.example/webhook"),
        [WebhookEventType.Create("payment-request.created")],
        "ref",
        now),
    "Plain HTTP webhook endpoints must be rejected.");

AssertThrows<ArgumentException>(
    () => WebhookSubscription.Create(
        ownerId,
        new Uri("https://user:pass@merchant.example/webhook"),
        [WebhookEventType.Create("payment-request.created")],
        "ref",
        now),
    "Webhook endpoint credentials must be rejected.");

AssertThrows<ArgumentException>(
    () => subscription.Disable(now.AddMinutes(4)),
    "Webhook lifecycle timestamps must not move backwards.");

AssertThrows<ArgumentException>(
    () => WebhookEventType.Create("payment request.created"),
    "Whitespace in event types must be rejected.");

Console.WriteLine("AFW-BE-WEBHOOK-MGMT-1 domain scenarios: PASS");
