using IdentityService.Api.Auth.Application;
using IdentityService.Api.Auth.Domain;
using IdentityService.Api.Auth.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

internal static class PersistenceCertification
{
    public static async Task RunAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AuthDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var user = new AuthUserEntity
        {
            Id = Guid.NewGuid(),
            NormalizedIdentifier = "user@example.com",
            PasswordHash = "password-hash",
            CreatedAtUtc = now,
        };
        var session = new AuthSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceId = "device-1",
            RefreshTokenHash = new string('a', 64),
            RefreshTokenFamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            TokenVersion = 1,
            Status = AuthSessionStatus.Active,
        };

        db.Users.Add(user);
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persistedUser = await db.Users.AsNoTracking().SingleAsync();
        var persistedSession = await db.Sessions.AsNoTracking().SingleAsync();

        Assert(persistedUser.NormalizedIdentifier == user.NormalizedIdentifier, "Auth user identifier persistence mismatch.");
        Assert(persistedSession.UserId == user.Id, "Auth session user relationship mismatch.");
        Assert(persistedSession.RefreshTokenHash == session.RefreshTokenHash, "Refresh token hash persistence mismatch.");
        Assert(persistedSession.Status == AuthSessionStatus.Active, "Auth session status persistence mismatch.");
        Assert(typeof(AuthSessionEntity).GetProperty("RefreshToken") is null, "Persistence model must not expose a raw refresh token field.");

        db.Users.Add(new AuthUserEntity
        {
            Id = Guid.NewGuid(),
            NormalizedIdentifier = user.NormalizedIdentifier,
            PasswordHash = "another-password-hash",
            CreatedAtUtc = now,
        });
        await AssertConstraintAsync(db, "Duplicate normalized identifiers must be rejected by the database.");

        db.Sessions.Add(new AuthSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceId = "device-2",
            RefreshTokenHash = session.RefreshTokenHash,
            RefreshTokenFamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            TokenVersion = 1,
            Status = AuthSessionStatus.Active,
        });
        await AssertConstraintAsync(db, "Duplicate refresh token hashes must be rejected by the database.");

        await CertifyEfStoresAsync(options, now);
        await CertifyConcurrentRefreshRotationAsync(now);

        Console.WriteLine("PASS: EF auth persistence model, stores, relational constraints and concurrent refresh rotation");
    }

    private static async Task CertifyEfStoresAsync(
        DbContextOptions<AuthDbContext> options,
        DateTimeOffset now)
    {
        await using var db = new AuthDbContext(options);
        var userStore = new EfAuthUserStore(db);
        var sessionStore = new EfAuthSessionStore(db);

        var authUser = new AuthUser(
            Guid.NewGuid(),
            "store-user@example.com",
            "store-password-hash",
            false,
            now);

        Assert(await userStore.TryAddAsync(authUser), "EF auth user store must add a new user.");
        Assert(!await userStore.TryAddAsync(authUser with { Id = Guid.NewGuid() }), "EF auth user store must reject duplicate normalized identifiers.");

        var loadedUser = await userStore.FindByNormalizedIdentifierAsync(authUser.NormalizedIdentifier);
        Assert(loadedUser == authUser, "EF auth user store round-trip mismatch.");

        var currentHash = new string('c', 64);
        var replacementHash = new string('d', 64);
        var authSession = new AuthSession(
            Guid.NewGuid(),
            authUser.Id,
            "store-device",
            currentHash,
            Guid.NewGuid(),
            now,
            now,
            now.AddDays(30),
            null,
            null,
            1,
            AuthSessionStatus.Active);

        await sessionStore.SaveAsync(authSession);

        var loadedSession = await sessionStore.GetByIdAsync(authSession.Id);
        Assert(loadedSession == authSession, "EF auth session store round-trip mismatch.");

        var byRefreshHash = await sessionStore.GetByRefreshTokenHashAsync(currentHash);
        Assert(byRefreshHash == authSession, "EF auth session lookup by refresh hash mismatch.");

        var rotatedAt = now.AddMinutes(5);
        var rotation = await sessionStore.TryRotateRefreshTokenAsync(currentHash, replacementHash, rotatedAt);
        Assert(rotation.Status == RefreshRotationStatus.Succeeded, "EF refresh rotation must succeed for the current token.");
        Assert(rotation.Session?.RefreshTokenHash == replacementHash, "EF refresh rotation must persist the replacement hash.");
        Assert(rotation.Session?.TokenVersion == 2, "EF refresh rotation must increment token version.");

        var oldHashLookup = await sessionStore.GetByRefreshTokenHashAsync(currentHash);
        Assert(oldHashLookup is null, "Consumed refresh hash must no longer resolve as current.");

        var reused = await sessionStore.TryRotateRefreshTokenAsync(currentHash, new string('e', 64), rotatedAt.AddMinutes(1));
        Assert(reused.Status == RefreshRotationStatus.Reused, "EF store must detect consumed refresh token reuse.");
        Assert(reused.Session?.Status == AuthSessionStatus.Revoked, "Refresh token reuse must revoke the compromised session.");
        Assert(reused.Session?.RevocationReason == "refresh_token_reuse", "Refresh token reuse revocation reason mismatch.");

        var persistedConsumedHash = await db.ConsumedRefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.RefreshTokenHash == currentHash);
        Assert(persistedConsumedHash.SessionId == authSession.Id, "Consumed refresh token must retain its session association.");

        var secondSession = authSession with
        {
            Id = Guid.NewGuid(),
            RefreshTokenHash = new string('f', 64),
            RefreshTokenFamilyId = Guid.NewGuid(),
            TokenVersion = 1,
            Status = AuthSessionStatus.Active,
            RevokedAtUtc = null,
            RevocationReason = null,
        };
        await sessionStore.SaveAsync(secondSession);
        var revokedAtUtc = now.AddMinutes(7);
        await sessionStore.RevokeAllForUserAsync(authUser.Id, "logout_all", revokedAtUtc);

        var revokedSecondSession = await sessionStore.GetByIdAsync(secondSession.Id);
        Assert(revokedSecondSession?.Status == AuthSessionStatus.Revoked, "EF logout-all must revoke active sessions for the user.");
        Assert(revokedSecondSession?.RevocationReason == "logout_all", "EF logout-all revocation reason mismatch.");
        Assert(revokedSecondSession?.RevokedAtUtc == revokedAtUtc, "EF logout-all must persist the explicit revocation timestamp.");
    }

    private static async Task CertifyConcurrentRefreshRotationAsync(DateTimeOffset now)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"afw-auth-rotation-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=5";
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var currentHash = new string('1', 64);
        var firstReplacementHash = new string('2', 64);
        var secondReplacementHash = new string('3', 64);
        var rotatedAtUtc = now.AddMinutes(10);

        try
        {
            await using (var setupDb = new AuthDbContext(options))
            {
                await setupDb.Database.EnsureCreatedAsync();
                setupDb.Users.Add(new AuthUserEntity
                {
                    Id = userId,
                    NormalizedIdentifier = $"concurrent-{userId:N}@example.com",
                    PasswordHash = "password-hash",
                    CreatedAtUtc = now,
                });
                setupDb.Sessions.Add(new AuthSessionEntity
                {
                    Id = sessionId,
                    UserId = userId,
                    DeviceId = "concurrent-device",
                    RefreshTokenHash = currentHash,
                    RefreshTokenFamilyId = Guid.NewGuid(),
                    CreatedAtUtc = now,
                    LastSeenAtUtc = now,
                    ExpiresAtUtc = now.AddDays(30),
                    TokenVersion = 1,
                    Status = AuthSessionStatus.Active,
                });
                await setupDb.SaveChangesAsync();
            }

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = RotateConcurrentlyAsync(options, start.Task, currentHash, firstReplacementHash, rotatedAtUtc);
            var second = RotateConcurrentlyAsync(options, start.Task, currentHash, secondReplacementHash, rotatedAtUtc);
            start.SetResult();

            var results = await Task.WhenAll(first, second);

            Assert(results.Count(status => status == RefreshRotationStatus.Succeeded) == 1,
                "Exactly one EF multi-DbContext refresh rotation must succeed.");
            Assert(results.Count(status => status == RefreshRotationStatus.Reused) == 1,
                "The losing EF multi-DbContext refresh rotation must resolve as reuse.");

            await using var verificationDb = new AuthDbContext(options);
            var persistedSession = await verificationDb.Sessions
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == sessionId);
            var consumedCount = await verificationDb.ConsumedRefreshTokens
                .AsNoTracking()
                .CountAsync(token => token.RefreshTokenHash == currentHash);

            Assert(persistedSession.TokenVersion == 2,
                "Concurrent EF rotation must advance token version exactly once.");
            Assert(consumedCount == 1,
                "Concurrent EF rotation must persist the consumed refresh token exactly once.");
            Assert(
                persistedSession.RefreshTokenHash == firstReplacementHash
                || persistedSession.RefreshTokenHash == secondReplacementHash,
                "Concurrent EF rotation must persist exactly one replacement refresh hash.");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static async Task<RefreshRotationStatus> RotateConcurrentlyAsync(
        DbContextOptions<AuthDbContext> options,
        Task start,
        string currentRefreshTokenHash,
        string replacementRefreshTokenHash,
        DateTimeOffset rotatedAtUtc)
    {
        await start;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            await using var db = new AuthDbContext(options);
            var store = new EfAuthSessionStore(db);

            try
            {
                var result = await store.TryRotateRefreshTokenAsync(
                    currentRefreshTokenHash,
                    replacementRefreshTokenHash,
                    rotatedAtUtc);
                return result.Status;
            }
            catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6)
            {
                await Task.Delay(25);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqliteException)
            {
                await Task.Delay(25);
            }
        }

        throw new InvalidOperationException("Concurrent EF refresh rotation did not settle after bounded retries.");
    }

    private static async Task AssertConstraintAsync(AuthDbContext db, string message)
    {
        try
        {
            await db.SaveChangesAsync();
            throw new InvalidOperationException(message);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
