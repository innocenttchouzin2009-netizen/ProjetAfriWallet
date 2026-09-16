using AfriWallet.PushNotifications.Application;
using AfriWallet.PushNotifications.Domain;
using AfriWallet.PushNotifications.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-push-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";
var userId = Guid.NewGuid();
var deviceId = "device-001";
var registeredAt = new DateTimeOffset(2026, 9, 16, 16, 0, 0, TimeSpan.Zero);
var rotatedAt = registeredAt.AddMinutes(5);
var revokedAt = registeredAt.AddMinutes(10);

try
{
    await using (var db = CreateDb(connectionString))
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        var repo = new EfPushDeviceRepository(db);
        var service = new PushDeviceRegistrationService(repo);

        var created = await service.RegisterAsync(new RegisterPushDeviceCommand(
            userId,
            deviceId,
            PushPlatform.Android,
            "token-v1",
            registeredAt));

        Assert(created.Status == RegisterPushDeviceStatus.Created, "First registration must be created.");
    }

    await using (var db = CreateDb(connectionString))
    {
        var repo = new EfPushDeviceRepository(db);
        var loaded = await repo.FindByUserAndDeviceAsync(userId, deviceId);
        Assert(loaded is not null, "Registration must survive DbContext restart.");
        Assert(loaded!.PushToken == "token-v1", "Push token must persist.");
        Assert(loaded.Platform == PushPlatform.Android, "Platform must persist.");
        Assert(loaded.Status == PushDeviceStatus.Active, "Status must persist.");

        var service = new PushDeviceRegistrationService(repo);
        var refreshed = await service.RegisterAsync(new RegisterPushDeviceCommand(
            userId,
            deviceId,
            PushPlatform.Ios,
            "token-v2",
            rotatedAt));
        Assert(refreshed.Status == RegisterPushDeviceStatus.Refreshed, "Existing device must refresh.");
    }

    await using (var db = CreateDb(connectionString))
    {
        var repo = new EfPushDeviceRepository(db);
        var rotated = await repo.FindByUserAndDeviceAsync(userId, deviceId);
        Assert(rotated is not null, "Rotated registration must exist.");
        Assert(rotated!.PushToken == "token-v2", "Rotated token must persist.");
        Assert(rotated.Platform == PushPlatform.Ios, "Rotated platform must persist.");
        Assert(rotated.UpdatedAtUtc == rotatedAt, "Rotation timestamp must persist.");

        rotated.Revoke(revokedAt);
        await repo.UpdateAsync(rotated);
    }

    await using (var db = CreateDb(connectionString))
    {
        var repo = new EfPushDeviceRepository(db);
        var revoked = await repo.FindByUserAndDeviceAsync(userId, deviceId);
        Assert(revoked is not null, "Revoked registration must remain persisted.");
        Assert(revoked!.Status == PushDeviceStatus.Revoked, "Revocation status must persist.");
        Assert(revoked.RevokedAtUtc == revokedAt, "Revocation timestamp must persist.");
        Assert(revoked.PushToken == "token-v2", "Revocation must not destroy current token.");

        var anotherUser = Guid.NewGuid();
        var second = PushDeviceRegistration.Register(
            anotherUser, deviceId, PushPlatform.Web, "token-other", registeredAt);
        await repo.AddAsync(second);
        var otherList = await repo.ListByUserAsync(anotherUser);
        Assert(otherList.Count == 1, "Same DeviceId must be allowed for a different user.");

        try
        {
            var duplicate = PushDeviceRegistration.Register(
                userId, deviceId, PushPlatform.Android, "duplicate-token", registeredAt.AddMinutes(20));
            await repo.AddAsync(duplicate);
            throw new InvalidOperationException("Expected durable unique (UserId, DeviceId) violation.");
        }
        catch (DbUpdateException)
        {
        }
    }

    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await using (var db = CreateDb(connectionString))
    {
        var repo = new EfPushDeviceRepository(db);
        try
        {
            await repo.ListByUserAsync(userId, cts.Token);
            throw new InvalidOperationException("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    Console.WriteLine("AFW-BE-PUSH-1 push device persistence scenarios: PASS");
}
finally
{
    SqliteConnection.ClearAllPools();
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

static PushNotificationDbContext CreateDb(string connectionString)
{
    var options = new DbContextOptionsBuilder<PushNotificationDbContext>()
        .UseSqlite(connectionString)
        .Options;
    return new PushNotificationDbContext(options);
}
