using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<NotificationPreferenceDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new NotificationPreferenceDbContext(options);
await db.Database.EnsureCreatedAsync();
var repository = new EfNotificationPreferenceRepository(db);

var userId = Guid.NewGuid();
var otherUserId = Guid.NewGuid();
var createdAt = new DateTimeOffset(2026, 9, 16, 18, 0, 0, TimeSpan.Zero);
var inAppPolicy = NotificationChannelPolicy.Create(NotificationChannel.InApp, true, true);
var pushPolicy = NotificationChannelPolicy.Create(NotificationChannel.Push, false, true);

var inApp = NotificationPreference.New(userId, inAppPolicy, createdAt);
await repository.AddAsync(inApp);

var loaded = await repository.GetAsync(userId, NotificationChannel.InApp);
Assert(loaded is not null, "Stored preference must be readable.");
Assert(loaded!.Id == inApp.Id, "Preference id must round-trip.");
Assert(loaded.UserId == userId, "User id must round-trip.");
Assert(loaded.Channel == NotificationChannel.InApp, "Channel must round-trip.");
Assert(loaded.IsEnabled, "Enabled state must round-trip.");
Assert(loaded.CreatedAtUtc == createdAt, "CreatedAtUtc must round-trip exactly.");
Assert(loaded.UpdatedAtUtc == createdAt, "UpdatedAtUtc must round-trip exactly.");

var push = NotificationPreference.New(userId, pushPolicy, createdAt.AddMinutes(1));
await repository.AddAsync(push);
var listed = await repository.ListByUserAsync(userId);
Assert(listed.Count == 2, "User must have two preferences.");
Assert(listed[0].Channel == NotificationChannel.InApp && listed[1].Channel == NotificationChannel.Push,
    "Preferences must be listed deterministically by channel.");

var updatedAt = createdAt.AddMinutes(10);
Assert(inApp.SetEnabled(false, inAppPolicy, updatedAt), "Preference should change state.");
await repository.UpdateAsync(inApp);
var updated = await repository.GetAsync(userId, NotificationChannel.InApp);
Assert(updated is not null && !updated.IsEnabled, "Updated enabled state must persist.");
Assert(updated!.CreatedAtUtc == createdAt, "Update must preserve CreatedAtUtc.");
Assert(updated.UpdatedAtUtc == updatedAt, "Update must preserve UpdatedAtUtc.");

Assert(await repository.GetAsync(otherUserId, NotificationChannel.InApp) is null,
    "Unknown user/channel must return null.");
Assert((await repository.ListByUserAsync(otherUserId)).Count == 0,
    "Unknown user must return empty list.");

var duplicate = NotificationPreference.New(userId, inAppPolicy, createdAt.AddHours(1));
try
{
    await repository.AddAsync(duplicate);
    throw new InvalidOperationException("Expected unique (UserId, Channel) constraint violation.");
}
catch (DbUpdateException) { }

var missing = NotificationPreference.New(otherUserId, pushPolicy, createdAt);
try
{
    await repository.UpdateAsync(missing);
    throw new InvalidOperationException("Expected missing preference update to fail.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.Ordinal)) { }

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await repository.ListByUserAsync(userId, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-NOTIFICATION-PREFERENCES-1 persistence scenarios: PASS");
