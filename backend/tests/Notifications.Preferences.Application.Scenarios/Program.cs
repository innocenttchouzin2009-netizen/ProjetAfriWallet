using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try { await action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}

var userId = Guid.NewGuid();
var now = new DateTimeOffset(2026, 9, 16, 18, 30, 0, TimeSpan.Zero);
var repo = new InMemoryRepository();
var policies = new FixedPolicyProvider();
var service = new NotificationPreferenceApplicationService(repo, policies);

var first = await service.GetAsync(userId);
Assert(first.Count == 2, "Reading preferences must resolve both channel policies.");
Assert(first.Single(x => x.Channel == NotificationChannel.InApp).IsEnabled, "In-app must use enabled default.");
Assert(first.Single(x => x.Channel == NotificationChannel.Push).IsEnabled, "Push must use enabled default.");
Assert(repo.AddCalls == 2, "Missing channel preferences must be initialized once.");

var second = await service.GetAsync(userId);
Assert(second.Count == 2 && repo.AddCalls == 2, "Repeated read must not duplicate initialized preferences.");

var pushUpdate = await service.UpdateAsync(new UpdateNotificationPreferenceCommand(
    userId, NotificationChannel.Push, false, now));
Assert(pushUpdate.Status == UpdateNotificationPreferenceStatus.Updated, "Existing push preference must update.");
Assert(!pushUpdate.Preference.IsEnabled, "Push preference must be disabled.");
Assert(repo.UpdateCalls == 1, "Changed preference must be persisted through UpdateAsync.");

var unchanged = await service.UpdateAsync(new UpdateNotificationPreferenceCommand(
    userId, NotificationChannel.Push, false, now.AddMinutes(1)));
Assert(unchanged.Status == UpdateNotificationPreferenceStatus.Unchanged, "Idempotent update must be unchanged.");
Assert(repo.UpdateCalls == 1, "Unchanged preference must not call UpdateAsync.");

var freshUser = Guid.NewGuid();
var created = await service.UpdateAsync(new UpdateNotificationPreferenceCommand(
    freshUser, NotificationChannel.Push, false, now));
Assert(created.Status == UpdateNotificationPreferenceStatus.Created, "Missing preference must be created on update.");
Assert(!created.Preference.IsEnabled, "Requested push state must be applied on creation.");
Assert(repo.AddCalls == 3, "Created preference must be persisted through AddAsync.");

await AssertThrowsAsync<InvalidOperationException>(
    () => service.UpdateAsync(new UpdateNotificationPreferenceCommand(
        userId, NotificationChannel.InApp, false, now.AddMinutes(2))),
    "Locked in-app policy must reject disabling.");

Assert(policies.GetCalls >= 3, "Updates must resolve channel policy through provider.");
Assert(policies.ListCalls >= 2, "Reads must resolve available channel policies through provider.");

using var cts = new CancellationTokenSource();
cts.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => service.GetAsync(userId, cts.Token),
    "Cancellation must propagate on read.");

Console.WriteLine("AFW-BE-NOTIFICATION-PREFERENCES-1 application orchestration scenarios: PASS");

sealed class FixedPolicyProvider : INotificationChannelPolicyProvider
{
    private readonly IReadOnlyList<NotificationChannelPolicy> policies = [
        NotificationChannelPolicy.Create(NotificationChannel.InApp, true, false),
        NotificationChannelPolicy.Create(NotificationChannel.Push, true, true)
    ];

    public int GetCalls { get; private set; }
    public int ListCalls { get; private set; }

    public NotificationChannelPolicy Get(NotificationChannel channel)
    {
        GetCalls++;
        return policies.Single(x => x.Channel == channel);
    }

    public IReadOnlyList<NotificationChannelPolicy> List()
    {
        ListCalls++;
        return policies;
    }
}

sealed class InMemoryRepository : INotificationPreferenceRepository
{
    private readonly Dictionary<(Guid, NotificationChannel), NotificationPreference> values = new();
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<NotificationPreference?> GetAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue((userId, channel), out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<NotificationPreference>>(values.Values.Where(x => x.UserId == userId).ToArray());
    }

    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[(preference.UserId, preference.Channel)] = preference;
        AddCalls++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[(preference.UserId, preference.Channel)] = preference;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}
