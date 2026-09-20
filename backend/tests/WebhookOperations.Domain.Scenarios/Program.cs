using AfriWallet.Merchants.Registry.Domain.Merchants;
using AfriWallet.Webhooks.Operations.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<T>(Action action, string message) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var now = new DateTimeOffset(2026, 9, 20, 15, 0, 0, TimeSpan.Zero);
var merchant = new MerchantId("afm-demo-001");
var initialSecret = new WebhookSigningSecretReference("vault://merchant/afm-demo-001/webhook/v1");
var retry = new RetryPolicyReference("webhook.standard.v1");
var delivery = new WebhookDeliveryConfiguration(TimeSpan.FromSeconds(10), retry, maxInFlight: 4);

var endpoint = WebhookEndpoint.Register(
    merchant,
    new Uri("https://merchant.example.com/hooks/afwal"),
    initialSecret,
    [
        new WebhookEventSubscription("payment.request.created"),
        new WebhookEventSubscription("payment.request.paid"),
        new WebhookEventSubscription("payment.request.created")
    ],
    delivery,
    now);

Assert(endpoint.Id.Value != Guid.Empty, "Endpoint id must be generated.");
Assert(endpoint.OwnerMerchantId == merchant, "Endpoint ownership must be immutable and preserved.");
Assert(endpoint.IsEnabled, "New endpoint must start enabled.");
Assert(endpoint.Subscriptions.Count == 2, "Duplicate subscriptions must be collapsed.");
Assert(endpoint.DeliveryConfiguration.RetryPolicy.Value == "webhook.standard.v1", "Retry policy reference must be preserved.");
Assert(endpoint.CreatedAtUtc == now && endpoint.UpdatedAtUtc == now, "Creation audit timestamps must be recorded.");

endpoint.Disable(now.AddMinutes(1));
Assert(!endpoint.IsEnabled && endpoint.DisabledAtUtc == now.AddMinutes(1), "Endpoint must disable with audit timestamp.");

endpoint.Enable(now.AddMinutes(2));
Assert(endpoint.IsEnabled && endpoint.EnabledAtUtc == now.AddMinutes(2), "Endpoint must re-enable with audit timestamp.");
Assert(endpoint.DisabledAtUtc is null, "Re-enabled endpoint must clear current disabled timestamp.");

var rotated = new WebhookSigningSecretReference("vault://merchant/afm-demo-001/webhook/v2");
endpoint.RotateSigningSecret(rotated, now.AddMinutes(3));
Assert(endpoint.SigningSecretReference == rotated, "Secret reference must rotate.");
Assert(endpoint.SecretRotatedAtUtc == now.AddMinutes(3), "Secret rotation audit timestamp must be recorded.");

endpoint.ReplaceSubscriptions(
    [new WebhookEventSubscription("transfer.completed")],
    now.AddMinutes(4));
Assert(endpoint.Subscriptions.SetEquals([new WebhookEventSubscription("transfer.completed")]), "Subscription replacement must be exact.");

var updatedDelivery = new WebhookDeliveryConfiguration(
    TimeSpan.FromSeconds(20),
    new RetryPolicyReference("webhook.high-value.v1"),
    2);
endpoint.UpdateDeliveryConfiguration(updatedDelivery, now.AddMinutes(5));
Assert(endpoint.DeliveryConfiguration == updatedDelivery, "Delivery configuration must update.");
Assert(endpoint.UpdatedAtUtc == now.AddMinutes(5), "Latest mutation must advance audit timestamp.");

AssertThrows<InvalidOperationException>(
    () => endpoint.RotateSigningSecret(rotated, now.AddMinutes(6)),
    "Secret rotation must reject reusing the active secret reference.");

AssertThrows<ArgumentException>(
    () => WebhookEndpoint.Register(
        merchant,
        new Uri("http://merchant.example.com/hooks"),
        initialSecret,
        [new WebhookEventSubscription("payment.request.created")],
        delivery,
        now),
    "Non-HTTPS endpoint must be rejected.");

AssertThrows<ArgumentException>(
    () => endpoint.Disable(now.AddMinutes(-1)),
    "Mutation timestamps must not move backwards.");

AssertThrows<ArgumentException>(
    () => new WebhookEventSubscription("Payment Request Created!"),
    "Invalid event subscription format must be rejected.");

AssertThrows<ArgumentOutOfRangeException>(
    () => new WebhookDeliveryConfiguration(TimeSpan.FromMilliseconds(500), retry),
    "Unsafe timeout must be rejected.");

Console.WriteLine("AFW-BE-WEBHOOK-OPS-1 endpoint registry and delivery configuration scenarios: PASS");
