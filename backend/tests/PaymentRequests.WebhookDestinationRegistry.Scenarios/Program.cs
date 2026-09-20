using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

var recipient = PaymentRequestWebhookRecipientId.From(Guid.NewGuid());
var policy = new PaymentRequestWebhookRetryPolicy(
    4,
    TimeSpan.FromSeconds(10),
    TimeSpan.FromMinutes(1));

var destination = PaymentRequestWebhookDestination.Create(
    recipient,
    new Uri("https://merchant.example/hooks/payment-requests"),
    [
        PaymentRequestLifecycleEventKind.Created,
        PaymentRequestLifecycleEventKind.Paid
    ],
    policy);

Assert(destination.IsActive, "New destination must be active by default.");
Assert(destination.IsSubscribedTo(PaymentRequestLifecycleEventKind.Created), "Created event must be subscribed.");
Assert(!destination.IsSubscribedTo(PaymentRequestLifecycleEventKind.Declined), "Unsubscribed event must not match.");
Assert(policy.DelayForAttempt(1) == TimeSpan.FromSeconds(10), "Attempt 1 retry delay mismatch.");
Assert(policy.DelayForAttempt(3) == TimeSpan.FromSeconds(40), "Attempt 3 retry delay mismatch.");
Assert(policy.DelayForAttempt(5) == TimeSpan.FromMinutes(1), "Retry delay must cap at max delay.");

destination.Deactivate();
Assert(!destination.IsSubscribedTo(PaymentRequestLifecycleEventKind.Created), "Inactive destination must never resolve subscriptions.");
destination.Activate();
destination.ReplaceSubscriptions([PaymentRequestLifecycleEventKind.Accepted]);
destination.ChangeRetryPolicy(new PaymentRequestWebhookRetryPolicy(
    2,
    TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(20)));
Assert(destination.IsSubscribedTo(PaymentRequestLifecycleEventKind.Accepted), "Updated subscription must apply.");
Assert(!destination.IsSubscribedTo(PaymentRequestLifecycleEventKind.Created), "Replaced subscription must be removed.");

AssertThrows<ArgumentException>(
    () => PaymentRequestWebhookDestination.Create(
        recipient,
        new Uri("ftp://merchant.example/hook"),
        [PaymentRequestLifecycleEventKind.Created]),
    "Non HTTP(S) destination must be rejected.");
AssertThrows<ArgumentException>(
    () => PaymentRequestWebhookDestination.Create(
        recipient,
        new Uri("https://user:secret@merchant.example/hook"),
        [PaymentRequestLifecycleEventKind.Created]),
    "Destination URL must not embed credentials.");
AssertThrows<ArgumentException>(
    () => PaymentRequestWebhookDestination.Create(
        recipient,
        new Uri("https://merchant.example/hook"),
        []),
    "Destination must subscribe to at least one lifecycle event.");

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<PaymentRequestWebhookRegistryDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new PaymentRequestWebhookRegistryDbContext(options);
await db.Database.EnsureCreatedAsync();

var repository = new EfPaymentRequestWebhookDestinationRegistry(db);
await repository.AddAsync(destination);

var loaded = await repository.GetAsync(destination.Id);
Assert(loaded is not null, "Persisted destination must be readable.");
Assert(loaded!.RecipientId == recipient, "Recipient identity must round-trip.");
Assert(loaded.Endpoint.AbsoluteUri == "https://merchant.example/hooks/payment-requests", "Endpoint must round-trip from authoritative registry.");
Assert(loaded.RetryPolicy.MaxAttempts == 2, "Retry policy must round-trip.");
Assert(loaded.IsSubscribedTo(PaymentRequestLifecycleEventKind.Accepted), "Subscription must round-trip.");

var second = PaymentRequestWebhookDestination.Create(
    recipient,
    new Uri("https://backup.example/payment-request-events"),
    [PaymentRequestLifecycleEventKind.Paid],
    PaymentRequestWebhookRetryPolicy.Default);
await repository.AddAsync(second);

var otherRecipient = PaymentRequestWebhookRecipientId.From(Guid.NewGuid());
var unrelated = PaymentRequestWebhookDestination.Create(
    otherRecipient,
    new Uri("https://other.example/events"),
    [PaymentRequestLifecycleEventKind.Paid]);
await repository.AddAsync(unrelated);

var paid = await repository.ResolveActiveForEventAsync(
    recipient,
    PaymentRequestLifecycleEventKind.Paid);
Assert(paid.Count == 1 && paid[0].Id == second.Id, "Only active subscribed destination for recipient/event must resolve.");

second.Deactivate();
await repository.UpdateAsync(second);
paid = await repository.ResolveActiveForEventAsync(
    recipient,
    PaymentRequestLifecycleEventKind.Paid);
Assert(paid.Count == 0, "Inactive destination must be excluded by authoritative resolution.");

var recipientDestinations = await repository.ListByRecipientAsync(recipient);
Assert(recipientDestinations.Count == 2, "Recipient registry listing must not leak other recipient destinations.");

var duplicateEndpoint = PaymentRequestWebhookDestination.Create(
    recipient,
    new Uri("https://merchant.example/hooks/payment-requests"),
    [PaymentRequestLifecycleEventKind.Paid]);
try
{
    await repository.AddAsync(duplicateEndpoint);
    throw new InvalidOperationException("Expected unique recipient/endpoint constraint.");
}
catch (DbUpdateException)
{
    db.ChangeTracker.Clear();
}

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await repository.ListByRecipientAsync(recipient, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException)
{
}

Console.WriteLine("AFW-BE-REQUEST-2 webhook destination registry and delivery policy scenarios: PASS");
