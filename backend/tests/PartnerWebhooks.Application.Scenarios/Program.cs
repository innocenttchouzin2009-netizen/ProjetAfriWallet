using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
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

var partner = PartnerId.From("partner.alpha");
var otherPartner = PartnerId.From("partner.beta");
var paymentSucceeded = WebhookEventType.From("payment.succeeded");
var transferFailed = WebhookEventType.From("transfer.failed");
var endpoint = WebhookEndpoint.From("https://partner.example/webhooks");
var secret = WebhookSigningSecretReference.From("vault://partners/alpha/webhook-v1");
var createdAt = new DateTimeOffset(2026, 9, 18, 11, 0, 0, TimeSpan.Zero);

var repository = new InMemoryRepository();
var service = new PartnerWebhookSubscriptionApplicationService(repository);

var created = await service.CreateAsync(new CreatePartnerWebhookSubscriptionCommand(
    partner,
    endpoint,
    secret,
    [paymentSucceeded, transferFailed, paymentSucceeded],
    createdAt));

Assert(created.Id.Value != Guid.Empty, "Created subscription id is required.");
Assert(created.PartnerId == partner, "Partner id must be preserved.");
Assert(created.Status == PartnerWebhookSubscriptionStatus.Active, "New subscription must be active.");
Assert(created.EventTypes.Count == 2, "Event types must be normalized by the domain.");
Assert(repository.AddCalls == 1, "Create must persist exactly once.");

var read = await service.GetAsync(created.Id, partner);
Assert(read is not null && read.Id == created.Id, "Owned subscription must be readable.");
Assert(await service.GetAsync(created.Id, otherPartner) is null, "Foreign partner must not read a subscription.");

var updatedAt = createdAt.AddMinutes(5);
var updated = await service.UpdateAsync(new UpdatePartnerWebhookSubscriptionCommand(
    created.Id,
    partner,
    WebhookEndpoint.From("https://partner.example/v2/webhooks"),
    WebhookSigningSecretReference.From("vault://partners/alpha/webhook-v2"),
    [paymentSucceeded],
    updatedAt));

Assert(updated is not null, "Owned subscription must be updatable.");
Assert(updated!.Endpoint.Value == "https://partner.example/v2/webhooks", "Endpoint update must be applied.");
Assert(updated.SigningSecretReference.Value.EndsWith("v2", StringComparison.Ordinal), "Secret reference rotation must be applied.");
Assert(updated.EventTypes.Count == 1 && updated.EventTypes[0] == paymentSucceeded, "Event type replacement must be applied.");
Assert(repository.UpdateCalls == 1, "Update must persist exactly once.");

Assert(await service.UpdateAsync(new UpdatePartnerWebhookSubscriptionCommand(
    created.Id,
    otherPartner,
    WebhookEndpoint.From("https://evil.example/webhooks"),
    null,
    null,
    updatedAt.AddMinutes(1))) is null, "Foreign partner must not update a subscription.");

await AssertThrowsAsync<ArgumentException>(
    () => service.UpdateAsync(new UpdatePartnerWebhookSubscriptionCommand(
        created.Id,
        partner,
        null,
        null,
        null,
        updatedAt.AddMinutes(1))),
    "Empty update must be rejected.");

var suspended = await service.SuspendAsync(created.Id, partner, updatedAt.AddMinutes(2));
Assert(suspended?.Status == PartnerWebhookSubscriptionStatus.Suspended, "Active subscription must suspend.");
Assert((await service.ListActiveByEventTypeAsync(paymentSucceeded)).Count == 0, "Suspended subscription must not be returned for delivery.");

var resumed = await service.ResumeAsync(created.Id, partner, updatedAt.AddMinutes(3));
Assert(resumed?.Status == PartnerWebhookSubscriptionStatus.Active, "Suspended subscription must resume.");
var activeForPayment = await service.ListActiveByEventTypeAsync(paymentSucceeded);
Assert(activeForPayment.Count == 1 && activeForPayment[0].Id == created.Id, "Active matching subscription must be returned.");
Assert((await service.ListActiveByEventTypeAsync(transferFailed)).Count == 0, "Non-subscribed event type must not match after replacement.");

Assert(await service.SuspendAsync(created.Id, otherPartner, updatedAt.AddMinutes(4)) is null, "Foreign partner must not suspend a subscription.");

var revoked = await service.RevokeAsync(created.Id, partner, updatedAt.AddMinutes(5));
Assert(revoked?.Status == PartnerWebhookSubscriptionStatus.Revoked, "Subscription must revoke.");
Assert((await service.ListActiveByEventTypeAsync(paymentSucceeded)).Count == 0, "Revoked subscription must not be returned for delivery.");

await AssertThrowsAsync<InvalidOperationException>(
    () => service.ResumeAsync(created.Id, partner, updatedAt.AddMinutes(6)),
    "Revoked subscription must remain terminal.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.ListActiveByEventTypeAsync(paymentSucceeded, cts.Token),
    "Cancellation must propagate.");

Console.WriteLine("AFW-BE-WEBHOOK-1 application and repository-port scenarios: PASS");

sealed class InMemoryRepository : IPartnerWebhookSubscriptionRepository
{
    private readonly Dictionary<Guid, PartnerWebhookSubscription> values = new();

    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<PartnerWebhookSubscription?> GetAsync(
        PartnerWebhookSubscriptionId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var subscription);
        return Task.FromResult(subscription);
    }

    public Task AddAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(subscription.Id.Value, subscription);
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.ContainsKey(subscription.Id.Value))
        {
            throw new InvalidOperationException("Webhook subscription was not found.");
        }

        values[subscription.Id.Value] = subscription;
        UpdateCalls++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PartnerWebhookSubscription>> ListActiveByEventTypeAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<PartnerWebhookSubscription> result = values.Values
            .Where(subscription => subscription.Accepts(eventType))
            .ToArray();
        return Task.FromResult(result);
    }
}
