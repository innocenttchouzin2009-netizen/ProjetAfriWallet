using AfriWallet.Notifications.Application;
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
var options = new DbContextOptionsBuilder<PushDeviceRegistrationDbContext>()
    .UseSqlite(connection)
    .Options;

await using (var setup = new PushDeviceRegistrationDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

var userId = Guid.NewGuid();
var otherUserId = Guid.NewGuid();
var createdAt = new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero);

await using (var db = new PushDeviceRegistrationDbContext(options))
{
    var repository = new EfPushDeviceRegistrationRepository(db);
    var service = new PushDeviceRegistrationService(repository);

    var registered = await service.RegisterAsync(new RegisterPushDeviceCommand(
        userId, "install-001", PushPlatform.Android, "token-v1", createdAt));
    Assert(registered.Status == RegisterPushDeviceStatus.Registered, "First registration must be Registered.");

    var existing = await service.RegisterAsync(new RegisterPushDeviceCommand(
        userId, "install-001", PushPlatform.Android, "token-v1", createdAt.AddMinutes(1)));
    Assert(existing.Status == RegisterPushDeviceStatus.Existing, "Same token must be Existing.");

    var rotated = await service.RegisterAsync(new RegisterPushDeviceCommand(
        userId, "install-001", PushPlatform.Android, "token-v2", createdAt.AddMinutes(2)));
    Assert(rotated.Status == RegisterPushDeviceStatus.TokenRotated, "Changed token must rotate.");
    Assert(rotated.Registration.PushToken == "token-v2", "Rotated token must be returned.");

    var active = await repository.ListActiveByUserAsync(userId);
    Assert(active.Count == 1 && active[0].PushToken == "token-v2", "Active user list must contain rotated token.");

    var unregistered = await service.UnregisterAsync(userId, "install-001", createdAt.AddMinutes(3));
    Assert(unregistered, "Active registration must unregister.");
    Assert((await repository.ListActiveByUserAsync(userId)).Count == 0, "Inactive registration must not be listed as active.");

    var reactivated = await service.RegisterAsync(new RegisterPushDeviceCommand(
        userId, "install-001", PushPlatform.Android, "token-v3", createdAt.AddMinutes(4)));
    Assert(reactivated.Status == RegisterPushDeviceStatus.Reactivated, "Inactive installation must reactivate.");
    Assert(reactivated.Registration.IsActive && reactivated.Registration.PushToken == "token-v3", "Reactivated registration must be active with new token.");

    try
    {
        await service.RegisterAsync(new RegisterPushDeviceCommand(
            otherUserId, "install-001", PushPlatform.Android, "other-token", createdAt.AddMinutes(5)));
        throw new InvalidOperationException("Expected ownership conflict.");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("another user", StringComparison.Ordinal)) { }
}

await using (var verify = new PushDeviceRegistrationDbContext(options))
{
    var repository = new EfPushDeviceRegistrationRepository(verify);
    var persisted = await repository.FindByInstallationIdAsync(" install-001 ");
    Assert(persisted is not null, "Registration must survive DbContext recreation.");
    Assert(persisted!.UserId == userId, "User id must round-trip.");
    Assert(persisted.PushToken == "token-v3", "Latest token must persist.");
    Assert(persisted.IsActive, "Reactivation must persist.");
    Assert(persisted.DeactivatedAtUtc is null, "Reactivation must clear deactivation timestamp.");

    var duplicate = PushDeviceRegistration.Create(
        userId,
        "install-001",
        PushPlatform.Android,
        "duplicate-token",
        createdAt.AddMinutes(6));
    try
    {
        await repository.AddAsync(duplicate);
        throw new InvalidOperationException("Expected durable InstallationId uniqueness violation.");
    }
    catch (DbUpdateException)
    {
        verify.ChangeTracker.Clear();
    }

    var second = PushDeviceRegistration.Create(
        userId,
        "install-002",
        PushPlatform.Ios,
        "ios-token",
        createdAt.AddMinutes(7));
    await repository.AddAsync(second);
    var active = await repository.ListActiveByUserAsync(userId);
    Assert(active.Count == 2, "Both active installations must be returned.");
    Assert(active.Select(x => x.InstallationId).SequenceEqual(new[] { "install-001", "install-002" }), "Active list must be deterministic by installation id.");
}

using (var cts = new CancellationTokenSource())
{
    cts.Cancel();
    await using var db = new PushDeviceRegistrationDbContext(options);
    var repository = new EfPushDeviceRegistrationRepository(db);
    try
    {
        await repository.ListActiveByUserAsync(userId, cts.Token);
        throw new InvalidOperationException("Expected cancellation.");
    }
    catch (OperationCanceledException) { }
}

Console.WriteLine("AFW-BE-NOTIFICATION-PUSH-1 persistence and registration repository scenarios: PASS");
