using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
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

var userId = Guid.NewGuid();
var occurredAt = new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.Zero);
var publishedAt = occurredAt.AddSeconds(5);
var sourceData = new Dictionary<string, string>
{
    ["walletId"] = Guid.NewGuid().ToString(),
    ["amountMinor"] = "2500"
};
var activityEvent = ActivityEvent.Create(
    userId,
    "wallet.transfer.completed",
    "Transfer completed",
    "Your transfer was completed.",
    occurredAt,
    sourceData);

sourceData["amountMinor"] = "9999";
Assert(activityEvent.Data["amountMinor"] == "2500", "ActivityEvent must defensively copy data.");

var defaultPolicies = new FixedPolicyProvider([
    NotificationChannelPolicy.Create(NotificationChannel.InApp, defaultEnabled: true, userConfigurable: false),
    NotificationChannelPolicy.Create(NotificationChannel.Push, defaultEnabled: true, userConfigurable: true)
]);

var noPreferences = new RecordingPreferenceRepository();
var publisher = new PreferenceAwareActivityEventPublisher(noPreferences, defaultPolicies);
var defaultResult = await publisher.PublishAsync(activityEvent, publishedAt);
Assert(defaultResult.EventId == activityEvent.EventId, "Publication result must preserve event id.");
Assert(defaultResult.Deliveries.Count == 2, "Default-enabled InApp and Push must both be routed.");
Assert(defaultResult.Deliveries[0].Channel == NotificationChannel.InApp, "Deliveries must be deterministically ordered by channel.");
Assert(defaultResult.Deliveries[1].Channel == NotificationChannel.Push, "Push delivery must follow InApp.");
Assert(defaultResult.Deliveries.All(x => x.EventId == activityEvent.EventId), "Delivery event ids must match.");
Assert(defaultResult.Deliveries.All(x => x.UserId == userId), "Delivery user ids must match.");
Assert(defaultResult.Deliveries.All(x => x.Kind == activityEvent.Kind), "Delivery kinds must match.");
Assert(noPreferences.GetCalls == 1, "Only configurable channels should query user preferences.");
Assert(noPreferences.AddCalls == 0 && noPreferences.UpdateCalls == 0, "Routing must not mutate preference persistence.");

var pushDisabledRepository = new RecordingPreferenceRepository([
    NotificationPreference.Restore(
        Guid.NewGuid(),
        userId,
        NotificationChannel.Push,
        false,
        occurredAt.AddMinutes(-1),
        occurredAt.AddMinutes(-1))
]);
var pushDisabledPublisher = new PreferenceAwareActivityEventPublisher(pushDisabledRepository, defaultPolicies);
var pushDisabled = await pushDisabledPublisher.PublishAsync(activityEvent, publishedAt);
Assert(pushDisabled.Deliveries.Count == 1, "Disabled Push preference must suppress Push delivery.");
Assert(pushDisabled.Deliveries[0].Channel == NotificationChannel.InApp, "Mandatory InApp delivery must remain enabled.");

var pushEnabledRepository = new RecordingPreferenceRepository([
    NotificationPreference.Restore(
        Guid.NewGuid(),
        userId,
        NotificationChannel.Push,
        true,
        occurredAt.AddMinutes(-1),
        occurredAt.AddMinutes(-1))
]);
var pushEnabledPublisher = new PreferenceAwareActivityEventPublisher(pushEnabledRepository, defaultPolicies);
var pushEnabled = await pushEnabledPublisher.PublishAsync(activityEvent, publishedAt);
Assert(pushEnabled.Deliveries.Count == 2, "Enabled Push preference must allow Push delivery.");

var defaultDisabledPolicies = new FixedPolicyProvider([
    NotificationChannelPolicy.Create(NotificationChannel.InApp, defaultEnabled: true, userConfigurable: false),
    NotificationChannelPolicy.Create(NotificationChannel.Push, defaultEnabled: false, userConfigurable: true)
]);
var defaultDisabled = await new PreferenceAwareActivityEventPublisher(
    new RecordingPreferenceRepository(),
    defaultDisabledPolicies).PublishAsync(activityEvent, publishedAt);
Assert(defaultDisabled.Deliveries.Count == 1, "Missing preference must fall back to channel policy default.");

var explicitlyEnabled = await new PreferenceAwareActivityEventPublisher(
    pushEnabledRepository,
    defaultDisabledPolicies).PublishAsync(activityEvent, publishedAt);
Assert(explicitlyEnabled.Deliveries.Count == 2, "Explicit preference must override configurable channel default.");

var emptyPolicies = await new PreferenceAwareActivityEventPublisher(
    new RecordingPreferenceRepository(),
    new FixedPolicyProvider([])).PublishAsync(activityEvent, publishedAt);
Assert(emptyPolicies.Deliveries.Count == 0, "No channel policies must produce no deliveries.");

await AssertThrowsAsync<InvalidOperationException>(
    () => new PreferenceAwareActivityEventPublisher(
        new RecordingPreferenceRepository(),
        new FixedPolicyProvider([
            NotificationChannelPolicy.Create(NotificationChannel.InApp, true, false),
            NotificationChannelPolicy.Create(NotificationChannel.InApp, true, false)
        ])).PublishAsync(activityEvent, publishedAt),
    "Duplicate channel policies must fail closed.");

await AssertThrowsAsync<ArgumentException>(
    () => publisher.PublishAsync(activityEvent, occurredAt.AddSeconds(-1)),
    "Publication before event occurrence must be rejected.");

using var cancellation = new CancellationTokenSource();
cancellation.Cancel();
await AssertThrowsAsync<OperationCanceledException>(
    () => publisher.PublishAsync(activityEvent, publishedAt, cancellation.Token),
    "Cancellation must propagate.");

var delivery = defaultResult.Deliveries.Single(x => x.Channel == NotificationChannel.Push);
Assert(delivery.RoutedAtUtc == publishedAt, "Delivery must record routing timestamp.");
Assert(delivery.Data["amountMinor"] == "2500", "Delivery must preserve activity event data.");

Console.WriteLine("AFW-BE-ACTIVITY-NOTIFICATION-1 activity event routing scenarios: PASS");

sealed class FixedPolicyProvider(IReadOnlyList<NotificationChannelPolicy> policies)
    : INotificationChannelPolicyProvider
{
    public NotificationChannelPolicy Get(NotificationChannel channel) =>
        policies.Single(policy => policy.Channel == channel);

    public IReadOnlyList<NotificationChannelPolicy> List() => policies;
}

sealed class RecordingPreferenceRepository(IEnumerable<NotificationPreference>? seed = null)
    : INotificationPreferenceRepository
{
    private readonly Dictionary<(Guid UserId, NotificationChannel Channel), NotificationPreference> values =
        (seed ?? Array.Empty<NotificationPreference>())
        .ToDictionary(x => (x.UserId, x.Channel));

    public int GetCalls { get; private set; }
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<NotificationPreference?> GetAsync(
        Guid userId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetCalls++;
        values.TryGetValue((userId, channel), out var preference);
        return Task.FromResult(preference);
    }

    public Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<NotificationPreference>>(
            values.Values.Where(x => x.UserId == userId).ToArray());
    }

    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddCalls++;
        values[(preference.UserId, preference.Channel)] = preference;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UpdateCalls++;
        values[(preference.UserId, preference.Channel)] = preference;
        return Task.CompletedTask;
    }
}
