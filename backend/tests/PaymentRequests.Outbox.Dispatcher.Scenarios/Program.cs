using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<PaymentRequestDbContext>().UseSqlite(connection).Options;
await using (var setup = new PaymentRequestDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

var t0 = new DateTimeOffset(2026, 9, 14, 20, 30, 0, TimeSpan.Zero);
var pendingA = NewMessage("payment-request.created.v1", t0);
var pendingB = NewMessage("payment-request.paid.v1", t0.AddMinutes(1));
var alreadyPublished = NewMessage("payment-request.declined.v1", t0.AddMinutes(2));
alreadyPublished.PublishedAtUtc = t0.AddMinutes(3).ToString("O");

await using (var db = new PaymentRequestDbContext(options))
{
    db.PaymentRequestIntegrationOutbox.AddRange(pendingA, pendingB, alreadyPublished);
    await db.SaveChangesAsync();
}

var time = new FixedTimeProvider(t0.AddMinutes(10));
var transport = new RecordingTransport(new Dictionary<Guid, int>
{
    [pendingA.Id] = 2,
    [pendingB.Id] = int.MaxValue
});

PaymentRequestOutboxDispatchResult first;
await using (var db = new PaymentRequestDbContext(options))
{
    var dispatcher = new PaymentRequestOutboxDispatcher(
        new EfPaymentRequestOutboxStore(db),
        transport,
        time,
        new PaymentRequestOutboxDispatchOptions(BatchSize: 10, MaxDeliveryAttempts: 3));

    first = await dispatcher.DispatchAsync();
}

Assert(first.PendingRead == 2, "Dispatcher must read only unpublished messages.");
Assert(first.Published == 1, "One retried message must publish successfully.");
Assert(first.Failed == 1, "One poison message must remain failed.");
Assert(transport.Attempts[pendingA.Id] == 3, "Transient delivery must retry up to success.");
Assert(transport.Attempts[pendingB.Id] == 3, "Persistent failure must stop at max attempts.");
Assert(!transport.Attempts.ContainsKey(alreadyPublished.Id), "Published messages must never be delivered again.");
Assert(transport.Keys[pendingA.Id].Distinct(StringComparer.Ordinal).Count() == 1,
    "Every retry must use the same idempotency key.");
Assert(transport.Keys[pendingA.Id][0] == pendingA.Id.ToString("N"),
    "Idempotency key must be derived from the stable message id.");

await using (var db = new PaymentRequestDbContext(options))
{
    var rows = await db.PaymentRequestIntegrationOutbox.AsNoTracking().ToDictionaryAsync(x => x.Id);
    Assert(rows[pendingA.Id].PublishedAtUtc == time.GetUtcNow().ToString("O"),
        "PublishedAtUtc must be written only after successful delivery.");
    Assert(rows[pendingB.Id].PublishedAtUtc is null,
        "Failed delivery must stay unpublished for a future retry.");
    Assert(rows[alreadyPublished.Id].PublishedAtUtc is not null,
        "Already published row must remain published.");
}

var recoveryTransport = new RecordingTransport(new Dictionary<Guid, int>());
time.Advance(TimeSpan.FromMinutes(5));
await using (var db = new PaymentRequestDbContext(options))
{
    var dispatcher = new PaymentRequestOutboxDispatcher(
        new EfPaymentRequestOutboxStore(db),
        recoveryTransport,
        time,
        new PaymentRequestOutboxDispatchOptions(BatchSize: 10, MaxDeliveryAttempts: 3));

    var retry = await dispatcher.DispatchAsync();
    Assert(retry.PendingRead == 1 && retry.Published == 1 && retry.Failed == 0,
        "A later dispatcher run must retry and publish the previously failed message.");
}

await using (var db = new PaymentRequestDbContext(options))
{
    var store = new EfPaymentRequestOutboxStore(db);
    var none = await store.ReadPendingAsync(10);
    Assert(none.Count == 0, "No messages may remain pending after successful retry.");

    await store.MarkPublishedAsync(pendingA.Id, time.GetUtcNow());
    var row = await db.PaymentRequestIntegrationOutbox.AsNoTracking().SingleAsync(x => x.Id == pendingA.Id);
    Assert(row.PublishedAtUtc == t0.AddMinutes(10).ToString("O"),
        "MarkPublishedAsync must be idempotent for an already published message.");
}

var batchOne = NewMessage("payment-request.cancelled.v1", t0.AddMinutes(20));
var batchTwo = NewMessage("payment-request.expired.v1", t0.AddMinutes(21));
await using (var db = new PaymentRequestDbContext(options))
{
    db.PaymentRequestIntegrationOutbox.AddRange(batchOne, batchTwo);
    await db.SaveChangesAsync();
}

await using (var db = new PaymentRequestDbContext(options))
{
    var dispatcher = new PaymentRequestOutboxDispatcher(
        new EfPaymentRequestOutboxStore(db),
        new RecordingTransport(new Dictionary<Guid, int>()),
        time,
        new PaymentRequestOutboxDispatchOptions(BatchSize: 1, MaxDeliveryAttempts: 1));

    var batch = await dispatcher.DispatchAsync();
    Assert(batch.PendingRead == 1 && batch.Published == 1,
        "Dispatcher must respect the configured batch size.");
}

using (var cts = new CancellationTokenSource())
{
    cts.Cancel();
    await using var db = new PaymentRequestDbContext(options);
    var dispatcher = new PaymentRequestOutboxDispatcher(
        new EfPaymentRequestOutboxStore(db),
        new RecordingTransport(new Dictionary<Guid, int>()),
        time,
        new PaymentRequestOutboxDispatchOptions());

    try
    {
        await dispatcher.DispatchAsync(cts.Token);
        throw new InvalidOperationException("Expected cancellation.");
    }
    catch (OperationCanceledException) { }
}

Console.WriteLine("AFW-BE-REQUEST-1 outbox dispatcher and delivery abstraction scenarios: PASS");

static PaymentRequestOutboxMessage NewMessage(string eventType, DateTimeOffset occurredAtUtc) => new()
{
    Id = Guid.NewGuid(),
    PaymentRequestId = Guid.NewGuid(),
    EventType = eventType,
    PayloadJson = "{\"version\":1}",
    OccurredAtUtc = occurredAtUtc.ToString("O"),
    PublishedAtUtc = null
};

sealed class RecordingTransport(IReadOnlyDictionary<Guid, int> failuresBeforeSuccess)
    : IPaymentRequestOutboxTransport
{
    public Dictionary<Guid, int> Attempts { get; } = new();
    public Dictionary<Guid, List<string>> Keys { get; } = new();

    public Task DeliverAsync(
        PaymentRequestOutboxDeliveryMessage message,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Attempts[message.MessageId] = Attempts.GetValueOrDefault(message.MessageId) + 1;
        if (!Keys.TryGetValue(message.MessageId, out var keys))
        {
            keys = [];
            Keys[message.MessageId] = keys;
        }
        keys.Add(idempotencyKey);

        var failures = failuresBeforeSuccess.GetValueOrDefault(message.MessageId);
        if (Attempts[message.MessageId] <= failures)
        {
            throw new InvalidOperationException("Simulated transport failure.");
        }

        return Task.CompletedTask;
    }
}

sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset current = utcNow;
    public override DateTimeOffset GetUtcNow() => current;
    public void Advance(TimeSpan value) => current = current.Add(value);
}
