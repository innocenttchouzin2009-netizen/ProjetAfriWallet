using AfriWallet.Webhooks.Application;
using AfriWallet.Webhooks.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var repository = new InMemoryWebhookSubscriptionRepository();
var service = new WebhookManagementService(repository);
var ownerId = Guid.NewGuid();
var foreignOwnerId = Guid.NewGuid();
var now = new DateTimeOffset(2026, 9, 18, 20, 30, 0, TimeSpan.Zero);

var registered = await service.RegisterAsync(new RegisterWebhookSubscriptionCommand(
    ownerId,
    new Uri("https://merchant.example/afwal/webhooks"),
    ["payment-request.created", "payment-request.paid"],
    "vault/afwal/merchant/webhook-key",
    now));

Assert(registered.OwnerId == ownerId, "Registered owner mismatch.");
Assert(registered.Status == WebhookSubscriptionStatus.Active, "Registered subscription must be active.");
Assert(repository.AddCalls == 1, "Registration must persist exactly once.");

var read = await service.GetOwnedAsync(ownerId, WebhookSubscriptionId.From(registered.Id));
Assert(read?.Id == registered.Id, "Owner must read its subscription.");

var hidden = await service.GetOwnedAsync(foreignOwnerId, WebhookSubscriptionId.From(registered.Id));
Assert(hidden is null, "Foreign subscription must be hidden.");

var list = await service.ListAsync(ownerId);
Assert(list.Count == 1 && list[0].Id == registered.Id, "Owner list must contain the registered subscription.");

var disabled = await service.DisableAsync(
    ownerId,
    WebhookSubscriptionId.From(registered.Id),
    now.AddMinutes(1));
Assert(disabled?.Status == WebhookSubscriptionStatus.Disabled, "Disable must persist status.");
Assert(repository.UpdateCalls == 1, "Disable must update exactly once.");

var enabled = await service.EnableAsync(
    ownerId,
    WebhookSubscriptionId.From(registered.Id),
    now.AddMinutes(2));
Assert(enabled?.Status == WebhookSubscriptionStatus.Active, "Enable must persist status.");
Assert(repository.UpdateCalls == 2, "Enable must update exactly once.");

var foreignDisable = await service.DisableAsync(
    foreignOwnerId,
    WebhookSubscriptionId.From(registered.Id),
    now.AddMinutes(3));
Assert(foreignDisable is null, "Foreign owner must not mutate the subscription.");
Assert(repository.UpdateCalls == 2, "Foreign mutation must not update storage.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.ListAsync(ownerId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException)
{
}

Console.WriteLine("AFW-BE-WEBHOOK-MGMT-1 application scenarios: PASS");

sealed class InMemoryWebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly Dictionary<Guid, WebhookSubscription> values = new();

    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<WebhookSubscription?> GetAsync(
        WebhookSubscriptionId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(id.Value, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<WebhookSubscription>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<WebhookSubscription>>(
            values.Values.Where(value => value.OwnerId == ownerId).ToArray());
    }

    public Task AddAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.Add(subscription.Id.Value, subscription);
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!values.ContainsKey(subscription.Id.Value))
            throw new InvalidOperationException("Webhook subscription was not found.");

        values[subscription.Id.Value] = subscription;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}
