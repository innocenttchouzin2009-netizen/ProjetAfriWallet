using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var userId = Guid.NewGuid();
var otherUserId = Guid.NewGuid();
var now = new DateTimeOffset(2026, 9, 17, 15, 0, 0, TimeSpan.Zero);
var repository = new InMemoryPreferenceRepository();
var policies = new FixedPolicyProvider();
var service = new NotificationDeliveryRoutingService(repository, policies);

var defaultPush = await service.EvaluateAsync(userId, NotificationChannel.Push);
Assert(defaultPush.IsEnabled, "Push must default to enabled when no user preference exists.");
Assert(defaultPush.Source == NotificationDeliveryDecisionSource.ChannelPolicy, "Missing push preference must use channel policy.");
Assert(repository.GetCalls == 1, "Configurable push channel must query preferences exactly once.");

var pushPolicy = policies.Get(NotificationChannel.Push);
var disabledPush = NotificationPreference.New(userId, pushPolicy, now);
disabledPush.SetEnabled(false, pushPolicy, now.AddMinutes(1));
repository.Seed(disabledPush);
var optedOut = await service.EvaluateAsync(userId, NotificationChannel.Push);
Assert(!optedOut.IsEnabled, "Disabled push preference must suppress delivery.");
Assert(optedOut.Source == NotificationDeliveryDecisionSource.UserPreference, "Stored push preference must drive routing.");

var otherUserDecision = await service.EvaluateAsync(otherUserId, NotificationChannel.Push);
Assert(otherUserDecision.IsEnabled, "Preferences must be scoped to the target user.");
Assert(otherUserDecision.Source == NotificationDeliveryDecisionSource.ChannelPolicy, "Other user must retain policy default.");

var callsBeforeInApp = repository.GetCalls;
var inApp = await service.EvaluateAsync(userId, NotificationChannel.InApp);
Assert(inApp.IsEnabled, "Mandatory in-app channel must remain enabled by policy.");
Assert(inApp.Source == NotificationDeliveryDecisionSource.ChannelPolicy, "Mandatory in-app routing must be policy-driven.");
Assert(repository.GetCalls == callsBeforeInApp, "Non-configurable channel must not query user preferences.");

try
{
    await service.EvaluateAsync(Guid.Empty, NotificationChannel.Push);
    throw new InvalidOperationException("Expected empty user id validation.");
}
catch (ArgumentException) { }

try
{
    await service.EvaluateAsync(userId, (NotificationChannel)999);
    throw new InvalidOperationException("Expected invalid channel validation.");
}
catch (ArgumentOutOfRangeException) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.EvaluateAsync(userId, NotificationChannel.Push, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-NOTIFICATION-ROUTING-1 preference-aware routing scenarios: PASS");

sealed class InMemoryPreferenceRepository : INotificationPreferenceRepository
{
    private readonly Dictionary<(Guid UserId, NotificationChannel Channel), NotificationPreference> values = new();
    public int GetCalls { get; private set; }

    public void Seed(NotificationPreference preference) => values[(preference.UserId, preference.Channel)] = preference;

    public Task<NotificationPreference?> GetAsync(Guid userId, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetCalls++;
        values.TryGetValue((userId, channel), out var preference);
        return Task.FromResult(preference);
    }

    public Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<NotificationPreference>>(values.Values.Where(x => x.UserId == userId).ToArray());
    }

    public Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Seed(preference);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Seed(preference);
        return Task.CompletedTask;
    }
}

sealed class FixedPolicyProvider : INotificationChannelPolicyProvider
{
    private readonly IReadOnlyList<NotificationChannelPolicy> policies = [
        NotificationChannelPolicy.Create(NotificationChannel.InApp, defaultEnabled: true, userConfigurable: false),
        NotificationChannelPolicy.Create(NotificationChannel.Push, defaultEnabled: true, userConfigurable: true)
    ];

    public NotificationChannelPolicy Get(NotificationChannel channel) => policies.Single(x => x.Channel == channel);
    public IReadOnlyList<NotificationChannelPolicy> List() => policies;
}
