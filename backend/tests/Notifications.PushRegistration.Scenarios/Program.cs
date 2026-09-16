using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<NotificationInboxDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new NotificationInboxDbContext(options);
await db.Database.EnsureCreatedAsync();

var key = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
var protector = new AesGcmPushTokenProtector(key);
var repository = new EfDevicePushRegistrationRepository(db, protector);
var service = new DevicePushRegistrationService(repository);

var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
var userOne = Guid.NewGuid();
var userTwo = Guid.NewGuid();

var first = await service.RegisterAsync(new RegisterDevicePushTokenCommand(
    userOne, "device-a", PushDevicePlatform.Android, "token-A-opaque", now));
Assert(first.Status == RegisterDevicePushTokenStatus.Created, "First registration must be created.");
Assert(first.Registration.UserId == userOne, "User binding mismatch.");

var replay = await service.RegisterAsync(new RegisterDevicePushTokenCommand(
    userOne, "device-a", PushDevicePlatform.Android, "token-A-opaque", now.AddMinutes(1)));
Assert(replay.Status == RegisterDevicePushTokenStatus.Existing, "Same device/token must be idempotent.");
Assert(replay.Registration.LastSeenAtUtc == now.AddMinutes(1), "Last-seen must advance on idempotent registration.");

var second = await service.RegisterAsync(new RegisterDevicePushTokenCommand(
    userOne, "device-b", PushDevicePlatform.Ios, "token-B-opaque", now.AddMinutes(2)));
Assert(second.Status == RegisterDevicePushTokenStatus.Created, "Second device must be independently registered.");
Assert((await service.ListActiveAsync(userOne)).Count == 2, "User must support multiple active devices.");

var replacement = await service.RegisterAsync(new RegisterDevicePushTokenCommand(
    userOne, "device-a", PushDevicePlatform.Android, "token-C-opaque", now.AddMinutes(3)));
Assert(replacement.Status == RegisterDevicePushTokenStatus.Replaced, "New token on same device must replace the active registration.");
Assert(await repository.GetActiveByTokenHashAsync(PushTokenFingerprint.Compute("token-A-opaque")) is null, "Replaced token must no longer be active.");
Assert((await service.ListActiveAsync(userOne)).Count == 2, "Replacing one device token must preserve the other device.");

var reassigned = await service.RegisterAsync(new RegisterDevicePushTokenCommand(
    userTwo, "device-z", PushDevicePlatform.Ios, "token-B-opaque", now.AddMinutes(4)));
Assert(reassigned.Status == RegisterDevicePushTokenStatus.Replaced, "A token moving to another account/device must revoke its previous active binding.");
Assert((await service.ListActiveAsync(userOne)).Count == 1, "Old owner must lose the migrated token registration.");
Assert((await service.ListActiveAsync(userTwo)).Count == 1, "New owner must receive the migrated token registration.");

Assert(!await service.RevokeAsync(userOne, "device-z", now.AddMinutes(5)), "A user must not revoke another user's device binding.");
Assert(await service.RevokeAsync(userTwo, "device-z", now.AddMinutes(5)), "Owner must be able to revoke own device token.");
Assert(!await service.RevokeAsync(userTwo, "device-z", now.AddMinutes(6)), "Repeated revoke must be idempotently absent.");

var stored = await db.DevicePushRegistrations.AsNoTracking().ToListAsync();
Assert(stored.Count >= 4, "Historical revoked registrations must be retained.");
Assert(stored.All(x => x.TokenHash.Length == 64), "Token fingerprints must be SHA-256 hex values.");
Assert(stored.All(x => !x.ProtectedToken.Contains("token-", StringComparison.Ordinal)), "Raw push tokens must not be stored in SQLite.");

var activeDeviceA = await repository.GetActiveByDeviceAsync(userOne, "device-a");
Assert(activeDeviceA is not null && activeDeviceA.Token == "token-C-opaque", "Protected token must round-trip for future transport usage.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.ListActiveAsync(userOne, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-PUSH-NOTIFICATION-1 device push registration and token persistence scenarios: PASS");
