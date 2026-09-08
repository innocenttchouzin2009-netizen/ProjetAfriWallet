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

        Console.WriteLine("PASS: EF auth persistence model and relational constraints");
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
