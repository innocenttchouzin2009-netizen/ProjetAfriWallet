using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

internal static class MigrationLifecycleCertification
{
    private const string InitialMigration = "20260908181500_InitialAuthPersistence";

    public static async Task RunAsync()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"afw-auth-migration-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        try
        {
            Assert(!File.Exists(databasePath), "Migration certification must start from a missing database file.");

            var userId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 9, 8, 18, 30, 0, TimeSpan.Zero);

            await using (var db = new AuthDbContext(options))
            {
                var pendingBefore = (await db.Database.GetPendingMigrationsAsync()).ToArray();
                Assert(pendingBefore.SequenceEqual([InitialMigration]), "A clean database must expose exactly the initial auth migration as pending.");

                await db.Database.MigrateAsync();

                Assert(File.Exists(databasePath), "Applying the initial migration must create the SQLite database file.");
                await AssertSchemaAsync(db);
                await AssertMigrationStateAsync(db, expectedAppliedCount: 1, expectedPendingCount: 0);

                db.Users.Add(new AuthUserEntity
                {
                    Id = userId,
                    NormalizedIdentifier = "migration-user@example.com",
                    PasswordHash = "migration-password-hash",
                    CreatedAtUtc = now,
                });
                db.Sessions.Add(new AuthSessionEntity
                {
                    Id = sessionId,
                    UserId = userId,
                    DeviceId = "migration-device",
                    RefreshTokenHash = new string('9', 64),
                    RefreshTokenFamilyId = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    LastSeenAtUtc = now,
                    ExpiresAtUtc = now.AddDays(30),
                    TokenVersion = 1,
                    Status = AuthSessionStatus.Active,
                });
                await db.SaveChangesAsync();
            }

            await using (var reopened = new AuthDbContext(options))
            {
                await reopened.Database.MigrateAsync();
                await AssertMigrationStateAsync(reopened, expectedAppliedCount: 1, expectedPendingCount: 0);

                var persistedUser = await reopened.Users.AsNoTracking().SingleAsync(user => user.Id == userId);
                var persistedSession = await reopened.Sessions.AsNoTracking().SingleAsync(session => session.Id == sessionId);

                Assert(persistedUser.NormalizedIdentifier == "migration-user@example.com", "Migrated database must preserve user data across context reopen.");
                Assert(persistedSession.UserId == userId, "Migrated database must preserve session relationships across context reopen.");

                var migrator = reopened.GetService<IMigrator>();
                await migrator.MigrateAsync("0");
                await AssertMigrationStateAsync(reopened, expectedAppliedCount: 0, expectedPendingCount: 1);
            }

            await using (var recreated = new AuthDbContext(options))
            {
                await recreated.Database.MigrateAsync();
                await AssertSchemaAsync(recreated);
                await AssertMigrationStateAsync(recreated, expectedAppliedCount: 1, expectedPendingCount: 0);
                Assert(!await recreated.Users.AnyAsync(), "Reapplying the initial migration after rollback must create an empty auth_users table.");
                Assert(!await recreated.Sessions.AnyAsync(), "Reapplying the initial migration after rollback must create an empty auth_sessions table.");
            }

            Console.WriteLine("PASS: EF migration lifecycle, schema history, reopen, rollback and reapply");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static async Task AssertMigrationStateAsync(
        AuthDbContext db,
        int expectedAppliedCount,
        int expectedPendingCount)
    {
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();

        Assert(applied.Length == expectedAppliedCount, $"Expected {expectedAppliedCount} applied auth migrations but found {applied.Length}.");
        Assert(pending.Length == expectedPendingCount, $"Expected {expectedPendingCount} pending auth migrations but found {pending.Length}.");

        if (expectedAppliedCount == 1)
        {
            Assert(applied.Single() == InitialMigration, "Applied migration id mismatch.");
        }

        if (expectedPendingCount == 1)
        {
            Assert(pending.Single() == InitialMigration, "Pending migration id mismatch.");
        }
    }

    private static async Task AssertSchemaAsync(AuthDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";

        var tables = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert(tables.Contains("__EFMigrationsHistory"), "EF migrations history table is missing.");
        Assert(tables.Contains("auth_users"), "auth_users table is missing after migration.");
        Assert(tables.Contains("auth_sessions"), "auth_sessions table is missing after migration.");
        Assert(tables.Contains("auth_consumed_refresh_tokens"), "auth_consumed_refresh_tokens table is missing after migration.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
