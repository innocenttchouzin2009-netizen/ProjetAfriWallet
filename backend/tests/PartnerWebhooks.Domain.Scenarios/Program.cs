using AfriWallet.PartnerWebhooks.Domain;

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

var createdAt = new DateTimeOffset(2026, 9, 18, 11, 0, 0, TimeSpan.Zero);
var partner = PartnerId.From("  partner-acme.eu  ");
var endpoint = WebhookEndpoint.From("https://hooks.partner.example/afwal/events?tenant=1");
var secretReference = WebhookSigningSecretReference.From("vault://partners/acme/webhook-signing-v1");
var createdEvent = WebhookEventType.From(" Payment-Request.Created ");
var paidEvent = WebhookEventType.From("payment-request.paid");

var subscription = PartnerWebhookSubscription.Create(
    partner,
    endpoint,
    secretReference,
    [paidEvent, createdEvent, createdEvent],
    createdAt);

Assert(subscription.Id.Value != Guid.Empty, "Subscription id must be generated.");
Assert(subscription.PartnerId.Value == "partner-acme.eu", "Partner id must be trimmed.");
Assert(subscription.Status == PartnerWebhookSubscriptionStatus.Active, "New subscription must be active.");
Assert(subscription.EventTypes.Count == 2, "Event types must be deduplicated.");
Assert(subscription.EventTypes[0].Value == "payment-request.created", "Event types must be normalized and ordered.");
Assert(subscription.Accepts(createdEvent), "Active subscription must accept configured events.");
Assert(!subscription.Accepts(WebhookEventType.From("payment-request.cancelled")), "Unconfigured event must not be accepted.");
Assert(subscription.SigningSecretReference.Value == "vault://partners/acme/webhook-signing-v1", "Only the secret reference must be retained.");

subscription.UpdateEndpoint(
    WebhookEndpoint.From("https://hooks.partner.example/v2/events"),
    createdAt.AddMinutes(1));
Assert(subscription.Endpoint.Value == "https://hooks.partner.example/v2/events", "Endpoint update must be applied.");

subscription.ReplaceEventTypes(
    [WebhookEventType.From("payment-request.cancelled"), paidEvent],
    createdAt.AddMinutes(2));
Assert(subscription.EventTypes.Count == 2, "Event replacement must be applied.");
Assert(!subscription.Accepts(createdEvent), "Removed event must no longer be accepted.");

subscription.RotateSigningSecretReference(
    WebhookSigningSecretReference.From("vault://partners/acme/webhook-signing-v2"),
    createdAt.AddMinutes(3));
Assert(subscription.SigningSecretReference.Value.EndsWith("-v2", StringComparison.Ordinal), "Secret reference rotation must be applied.");

subscription.Suspend(createdAt.AddMinutes(4));
Assert(subscription.Status == PartnerWebhookSubscriptionStatus.Suspended, "Subscription must suspend.");
Assert(!subscription.Accepts(paidEvent), "Suspended subscription must not accept delivery.");

subscription.Resume(createdAt.AddMinutes(5));
Assert(subscription.Status == PartnerWebhookSubscriptionStatus.Active, "Subscription must resume.");
Assert(subscription.Accepts(paidEvent), "Resumed subscription must accept configured delivery.");

subscription.Revoke(createdAt.AddMinutes(6));
Assert(subscription.Status == PartnerWebhookSubscriptionStatus.Revoked, "Subscription must revoke.");
Assert(!subscription.Accepts(paidEvent), "Revoked subscription must never accept delivery.");
AssertThrows<InvalidOperationException>(
    () => subscription.UpdateEndpoint(WebhookEndpoint.From("https://other.example/events"), createdAt.AddMinutes(7)),
    "Revoked subscription must be immutable.");
AssertThrows<InvalidOperationException>(
    () => subscription.Resume(createdAt.AddMinutes(7)),
    "Revoked subscription cannot resume.");

AssertThrows<ArgumentException>(() => PartnerId.From("bad partner"), "Partner id whitespace must be rejected.");
AssertThrows<ArgumentException>(() => WebhookEndpoint.From("http://partner.example/events"), "HTTP endpoint must be rejected.");
AssertThrows<ArgumentException>(() => WebhookEndpoint.From("https://user:pass@partner.example/events"), "Endpoint credentials must be rejected.");
AssertThrows<ArgumentException>(() => WebhookEndpoint.From("https://partner.example/events#secret"), "Endpoint fragments must be rejected.");
AssertThrows<ArgumentException>(() => WebhookEventType.From("payment request.created"), "Event whitespace must be rejected.");
AssertThrows<ArgumentException>(() => WebhookSigningSecretReference.From("vault secret"), "Secret reference whitespace must be rejected.");
AssertThrows<ArgumentException>(
    () => PartnerWebhookSubscription.Create(partner, endpoint, secretReference, [], createdAt),
    "Subscription must require at least one event.");
AssertThrows<ArgumentException>(
    () => PartnerWebhookSubscription.Create(
        partner,
        endpoint,
        secretReference,
        Enumerable.Range(0, 65).Select(index => WebhookEventType.From($"event.{index}")),
        createdAt),
    "Subscription must cap event types.");
AssertThrows<ArgumentException>(
    () => PartnerWebhookSubscription.Create(partner, endpoint, secretReference, [createdEvent], createdAt.ToOffset(TimeSpan.FromHours(2))),
    "Creation timestamp must be UTC.");

var chronological = PartnerWebhookSubscription.Create(partner, endpoint, secretReference, [createdEvent], createdAt);
chronological.Suspend(createdAt.AddMinutes(2));
AssertThrows<ArgumentException>(
    () => chronological.Resume(createdAt.AddMinutes(1)),
    "Lifecycle timestamp must not move backwards.");

Console.WriteLine("AFW-BE-WEBHOOK-1 webhook domain and subscription foundation scenarios: PASS");
