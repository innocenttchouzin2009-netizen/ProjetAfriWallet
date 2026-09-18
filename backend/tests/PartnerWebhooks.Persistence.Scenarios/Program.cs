using AfriWallet.PartnerWebhooks.Domain;
using AfriWallet.PartnerWebhooks.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<PartnerWebhookDbContext>()
    .UseSqlite(connection)
    .Options;

await using (var setup = new PartnerWebhookDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

var partner = PartnerId.From("partner.alpha");
var endpoint = WebhookEndpoint.From("https://partner.example/webhooks");
var secret = WebhookSigningSecretReference.From("vault://partners/alpha/webhook-v1");
var paymentSucceeded = WebhookEventType.From("payment.succeeded");
var transferFailed = WebhookEventType.From("transfer.failed");
var createdAt = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

var subscription = PartnerWebhookSubscription.Create(
    partner,
    endpoint,
    secret,
    [paymentSucceeded, transferFailed, paymentSucceeded],
    createdAt);

await using (var db = new PartnerWebhookDbContext(options))
{
    var repository = new EfPartnerWebhookSubscriptionRepository(db);
    await repository.AddAsync(subscription);
}

await using (var db = new PartnerWebhookDbContext(options))
{
    var repository = new EfPartnerWebhookSubscriptionRepository(db);
    var roundTrip = await repository.GetAsync(subscription.Id);

    Assert(roundTrip is not null, "Stored subscription must be readable.");
    Assert(roundTrip!.Id == subscription.Id, "Subscription id must round-trip.");
    Assert(roundTrip.PartnerId == partner, "Partner id must round-trip.");
    Assert(roundTrip.Endpoint == endpoint, "Endpoint must round-trip.");
    Assert(roundTrip.SigningSecretReference == secret, "Secret reference must round-trip.");
    Assert(roundTrip.Status == PartnerWebhookSubscriptionStatus.Active, "Status must round-trip.");
    Assert(roundTrip.EventTypes.Count == 2, "Normalized event types must round-trip.");
    Assert(roundTrip.EventTypes.Contains(paymentSucceeded), "Payment event type must round-trip.");
    Assert(roundTrip.EventTypes.Contains(transferFailed), "Transfer event type must round-trip.");
    Assert(roundTrip.CreatedAtUtc == createdAt, "CreatedAt must round-trip.");
    Assert(roundTrip.UpdatedAtUtc == createdAt, "UpdatedAt must round-trip.");

    var active = await repository.ListActiveByEventTypeAsync(paymentSucceeded);
    Assert(active.Count == 1 && active[0].Id == subscription.Id, "Active matching subscription must be listed.");

    roundTrip.Suspend(createdAt.AddMinutes(5));
    await repository.UpdateAsync(roundTrip);
}

await using (var db = new PartnerWebhookDbContext(options))
{
    var repository = new EfPartnerWebhookSubscriptionRepository(db);
    var suspended = await repository.GetAsync(subscription.Id);
    Assert(suspended?.Status == PartnerWebhookSubscriptionStatus.Suspended, "Suspended status must persist.");
    Assert(suspended?.UpdatedAtUtc == createdAt.AddMinutes(5), "Suspension timestamp must persist.");
    Assert((await repository.ListActiveByEventTypeAsync(paymentSucceeded)).Count == 0, "Suspended subscription must not be listed.");

    suspended!.Resume(createdAt.AddMinutes(6));
    suspended.ReplaceEventTypes([paymentSucceeded], createdAt.AddMinutes(7));
    suspended.RotateSigningSecretReference(
        WebhookSigningSecretReference.From("vault://partners/alpha/webhook-v2"),
        createdAt.AddMinutes(8));
    suspended.UpdateEndpoint(WebhookEndpoint.From("https://partner.example/v2/webhooks"), createdAt.AddMinutes(9));
    await repository.UpdateAsync(suspended);
}

await using (var db = new PartnerWebhookDbContext(options))
{
    var repository = new EfPartnerWebhookSubscriptionRepository(db);
    var updated = await repository.GetAsync(subscription.Id);
    Assert(updated?.Status == PartnerWebhookSubscriptionStatus.Active, "Resumed status must persist.");
    Assert(updated?.Endpoint.Value == "https://partner.example/v2/webhooks", "Endpoint update must persist.");
    Assert(updated?.SigningSecretReference.Value.EndsWith("v2", StringComparison.Ordinal) == true, "Secret reference rotation must persist.");
    Assert(updated?.EventTypes.Count == 1 && updated.EventTypes[0] == paymentSucceeded, "Event replacement must persist.");
    Assert((await repository.ListActiveByEventTypeAsync(transferFailed)).Count == 0, "Removed event type must not match.");
    Assert((await repository.ListActiveByEventTypeAsync(paymentSucceeded)).Count == 1, "Remaining event type must match.");

    updated!.Revoke(createdAt.AddMinutes(10));
    await repository.UpdateAsync(updated);
}

await using (var db = new PartnerWebhookDbContext(options))
{
    var repository = new EfPartnerWebhookSubscriptionRepository(db);
    var revoked = await repository.GetAsync(subscription.Id);
    Assert(revoked?.Status == PartnerWebhookSubscriptionStatus.Revoked, "Revoked status must persist.");
    Assert((await repository.ListActiveByEventTypeAsync(paymentSucceeded)).Count == 0, "Revoked subscription must never be delivered.");

    await AssertThrowsAsync<InvalidOperationException>(
        () => repository.UpdateAsync(PartnerWebhookSubscription.Create(
            PartnerId.From("partner.missing"),
            WebhookEndpoint.From("https://missing.example/webhooks"),
            WebhookSigningSecretReference.From("vault://missing/webhook"),
            [paymentSucceeded],
            createdAt)),
        "Updating an unknown subscription must fail closed.");
}

using var cts = new CancellationTokenSource();
cts.Cancel();
await using (var db = new PartnerWebhookDbContext(options))
{
    var repository = new EfPartnerWebhookSubscriptionRepository(db);
    await AssertThrowsAsync<OperationCanceledException>(
        () => repository.ListActiveByEventTypeAsync(paymentSucceeded, cts.Token),
        "Cancellation must propagate.");
}

Console.WriteLine("AFW-BE-WEBHOOK-1 persistence and repository adapter scenarios: PASS");
