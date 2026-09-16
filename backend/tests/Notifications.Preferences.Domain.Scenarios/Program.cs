using AfriWallet.Notifications.Domain;

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

var now = new DateTimeOffset(2026, 9, 16, 17, 0, 0, TimeSpan.Zero);
var userId = Guid.NewGuid();
var pushPolicy = NotificationChannelPolicy.Create(NotificationChannel.Push, defaultEnabled: true, userConfigurable: true);
var inAppLockedPolicy = NotificationChannelPolicy.Create(NotificationChannel.InApp, defaultEnabled: true, userConfigurable: false);

var preference = NotificationPreference.New(userId, pushPolicy, now);
Assert(preference.Id != Guid.Empty, "Preference id must be generated.");
Assert(preference.UserId == userId, "User id must be preserved.");
Assert(preference.Channel == NotificationChannel.Push, "Channel must be preserved.");
Assert(preference.IsEnabled, "Default policy state must initialize preference.");
Assert(preference.CreatedAtUtc == now && preference.UpdatedAtUtc == now, "Creation timestamps must match.");

var changed = preference.SetEnabled(false, pushPolicy, now.AddMinutes(1));
Assert(changed, "Configurable channel must allow state change.");
Assert(!preference.IsEnabled, "Preference must be disabled.");
Assert(preference.UpdatedAtUtc == now.AddMinutes(1), "Update timestamp must be recorded.");

var unchanged = preference.SetEnabled(false, pushPolicy, now.AddMinutes(2));
Assert(!unchanged, "Idempotent state update must report no change.");
Assert(preference.UpdatedAtUtc == now.AddMinutes(1), "Idempotent update must not mutate timestamp.");

var inApp = NotificationPreference.New(userId, inAppLockedPolicy, now);
Assert(inApp.IsEnabled, "Non-configurable channel must use policy default.");
AssertThrows<InvalidOperationException>(
    () => inApp.SetEnabled(false, inAppLockedPolicy, now.AddMinutes(1)),
    "Non-configurable channel must reject deviation from policy default.");

var restored = NotificationPreference.Restore(
    Guid.NewGuid(),
    userId,
    NotificationChannel.Push,
    false,
    now,
    now.AddMinutes(5));
Assert(!restored.IsEnabled && restored.Channel == NotificationChannel.Push, "Restore must preserve persisted state.");

AssertThrows<InvalidOperationException>(
    () => preference.SetEnabled(true, inAppLockedPolicy, now.AddMinutes(3)),
    "Mismatched channel policy must be rejected.");
AssertThrows<ArgumentException>(
    () => NotificationPreference.New(Guid.Empty, pushPolicy, now),
    "Empty user id must be rejected.");
AssertThrows<ArgumentException>(
    () => NotificationPreference.Restore(Guid.Empty, userId, NotificationChannel.Push, true, now, now),
    "Empty preference id must be rejected.");
AssertThrows<ArgumentException>(
    () => NotificationPreference.Restore(Guid.NewGuid(), userId, NotificationChannel.Push, true, now, now.AddMinutes(-1)),
    "Restore timestamp must be monotonic.");
AssertThrows<ArgumentException>(
    () => preference.SetEnabled(true, pushPolicy, now.AddMinutes(-1)),
    "Update timestamp must not move backwards.");
AssertThrows<ArgumentException>(
    () => NotificationPreference.New(userId, pushPolicy, now.ToOffset(TimeSpan.FromHours(2))),
    "Creation timestamp must be UTC.");
AssertThrows<ArgumentOutOfRangeException>(
    () => NotificationChannelPolicy.Create((NotificationChannel)999, true, true),
    "Unknown channel must be rejected by policy.");
AssertThrows<ArgumentOutOfRangeException>(
    () => NotificationPreference.Restore(Guid.NewGuid(), userId, (NotificationChannel)999, true, now, now),
    "Unknown channel must be rejected by preference restore.");

Console.WriteLine("AFW-BE-NOTIFICATION-PREFERENCES-1 domain and channel policy scenarios: PASS");
