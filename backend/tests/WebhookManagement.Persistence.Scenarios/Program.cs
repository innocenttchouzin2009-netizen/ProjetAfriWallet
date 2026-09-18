using AfriWallet.Webhooks.Domain;
using AfriWallet.Webhooks.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<WebhookManagementDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new WebhookManagementDbContext(options);
await db.Database.EnsureCreatedAsync();

var repository = new EfWebhookSubscriptionRepository(db);
var ownerId = Guid.NewGuid();
var otherOwnerId = Guid.NewGuid();
var createdAt = new DateTimeOffset(2026, 9, 18, 20, 30, 0, TimeSpan.Zero);

var first = WebhookSubscription.Create(
    ownerId,
    new Uri("https://merchant.example/webhooks/payments"),
    [WebhookEventType.Create("payment.completed"), WebhookEventType.Create("payment.failed")],
    "keyref:merchant:primary",
    createdAt);
await repository.AddAsync(first);

var loaded = await repository.GetAsync(first.Id);
Assert(loaded is not null, "Persisted webhook subscription must be readable.");
Assert(loaded!.OwnerId == ownerId, "Owner id must round-trip.");
Assert(loaded.Endpoint.AbsoluteUri == first.Endpoint.AbsoluteUri, "Endpoint must round-trip.");
Assert(loaded.SigningKeyReference == "keyref:merchant:primary", "Signing key reference must round-trip.");
Assert(loaded.EventTypes.Select(x => x.Value).SequenceEqual(["payment.completed", "payment.failed"]),
    "Event types must round-trip in canonical order.");
Assert(loaded.Status == WebhookSubscriptionStatus.Active, "New persisted subscription must remain active.");

var second = WebhookSubscription.Create(
    ownerId,
    new Uri("https://merchant.example/webhooks/refunds"),
    [WebhookEventType.Create("refund.completed")],
    "keyref:merchant:secondary",
    createdAt.AddMinutes(1));
await repository.AddAsync(second);

var foreign = WebhookSubscription.Create(
    otherOwnerId,
    new Uri("https://other.example/webhooks"),
    [WebhookEventType.Create("payment.completed")],
    "keyref:other:primary",
    createdAt);
await repository.AddAsync(foreign);

var ownerSubscriptions = await repository.ListByOwnerAsync(ownerId);
Assert(ownerSubscriptions.Count == 2, "Owner listing must return only that owner's subscriptions.");
Assert(ownerSubscriptions[0].Id == first.Id && ownerSubscriptions[1].Id == second.Id,
    "Owner listing must be deterministic.");
Assert(ownerSubscriptions.All(x => x.OwnerId == ownerId),
    "Foreign subscriptions must not leak into owner listing.");

first.Disable(createdAt.AddMinutes(2));
await repository.UpdateAsync(first);

var disabled = await repository.GetAsync(first.Id);
Assert(disabled?.Status == WebhookSubscriptionStatus.Disabled, "Disabled status must persist.");
Assert(disabled?.UpdatedAtUtc == createdAt.AddMinutes(2), "Updated timestamp must persist.");
Assert(disabled?.EventTypes.Select(x => x.Value).SequenceEqual(["payment.completed", "payment.failed"]) == true,
    "Status updates must preserve event types.");
Assert(disabled?.SigningKeyReference == "keyref:merchant:primary",
    "Status updates must preserve signing key reference.");

first.Enable(createdAt.AddMinutes(3));
first.ChangeEndpoint(new Uri("https://merchant.example/webhooks/v2"), createdAt.AddMinutes(4));
first.ReplaceEventTypes(
    [WebhookEventType.Create("payment.refunded"), WebhookEventType.Create("payment.completed")],
    createdAt.AddMinutes(5));
first.ChangeSigningKeyReference("keyref:merchant:rotated", createdAt.AddMinutes(6));
await repository.UpdateAsync(first);

var updated = await repository.GetAsync(first.Id);
Assert(updated?.Status == WebhookSubscriptionStatus.Active, "Re-enabled status must persist.");
Assert(updated?.Endpoint.AbsoluteUri == "https://merchant.example/webhooks/v2",
    "Changed endpoint must persist.");
Assert(updated?.EventTypes.Select(x => x.Value).SequenceEqual(["payment.completed", "payment.refunded"]) == true,
    "Replaced event types must persist canonically.");
Assert(updated?.SigningKeyReference == "keyref:merchant:rotated",
    "Changed signing key reference must persist.");

var duplicate = WebhookSubscription.Restore(
    first.Id,
    Guid.NewGuid(),
    new Uri("https://duplicate.example/webhook"),
    [WebhookEventType.Create("payment.completed")],
    "keyref:duplicate",
    WebhookSubscriptionStatus.Active,
    createdAt,
    createdAt);

try
{
    await repository.AddAsync(duplicate);
    throw new InvalidOperationException("Expected duplicate id rejection.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("already persisted", StringComparison.Ordinal)) { }

Assert(await repository.GetAsync(WebhookSubscriptionId.New()) is null,
    "Unknown subscription must return null.");

try
{
    await repository.ListByOwnerAsync(Guid.Empty);
    throw new InvalidOperationException("Expected empty owner rejection.");
}
catch (ArgumentException) { }

var missingUpdate = WebhookSubscription.Create(
    ownerId,
    new Uri("https://merchant.example/missing"),
    [WebhookEventType.Create("payment.completed")],
    "keyref:missing",
    createdAt);
try
{
    await repository.UpdateAsync(missingUpdate);
    throw new InvalidOperationException("Expected missing update rejection.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.Ordinal)) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await repository.GetAsync(first.Id, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

var indexes = await db.Database.SqlQueryRaw<string>(
    "SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name='WebhookSubscriptions' ORDER BY name")
    .ToListAsync();
Assert(indexes.Count >= 2, "Webhook subscription owner/status and owner/order indexes must exist.");

Console.WriteLine("AFW-BE-WEBHOOK-MGMT-1 persistence scenarios: PASS");
