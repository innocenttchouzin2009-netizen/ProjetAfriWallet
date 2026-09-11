using AfriWallet.P2P.Directory.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var options = new DbContextOptionsBuilder<RecipientDirectoryDbContext>()
    .UseSqlite(connection)
    .Options;

await using var db = new RecipientDirectoryDbContext(options);
await db.Database.EnsureCreatedAsync();

var afWalOwner = Guid.NewGuid();
var qrOwner = Guid.NewGuid();
const string afWalId = "recipient.one";
const string qrToken = "opaque-qr-token-commit5";
var qrHash = RecipientDirectoryNormalization.HashQrToken(qrToken);

db.AfWalIdentities.Add(new AfWalIdentityEntry
{
    Id = Guid.NewGuid(),
    OwnerId = afWalOwner,
    AfWalId = RecipientDirectoryNormalization.NormalizeAfWalId(afWalId),
    IsActive = true
});
db.QrRecipients.Add(new QrRecipientEntry
{
    Id = Guid.NewGuid(),
    OwnerId = qrOwner,
    TokenHash = qrHash,
    IsActive = true
});
await db.SaveChangesAsync();

var afWalDirectory = new EfAfWalIdentityDirectory(db);
var qrDirectory = new EfQrRecipientDirectory(db);

await RunAsync("AfWal ID resolves authoritative owner", async () =>
{
    var owner = await afWalDirectory.ResolveOwnerIdAsync(afWalId);
    Assert(owner == afWalOwner, "AfWal ID owner mismatch.");
});

await RunAsync("AfWal ID uses domain trim normalization", async () =>
{
    var owner = await afWalDirectory.ResolveOwnerIdAsync("  recipient.one  ");
    Assert(owner == afWalOwner, "Trim-normalized AfWal ID should resolve.");
});

await RunAsync("unknown AfWal ID returns null", async () =>
{
    var owner = await afWalDirectory.ResolveOwnerIdAsync("missing.id");
    Assert(owner is null, "Unknown AfWal ID must not resolve.");
});

await RunAsync("QR token resolves through SHA-256 hash", async () =>
{
    var owner = await qrDirectory.ResolveOwnerIdAsync(qrToken);
    Assert(owner == qrOwner, "QR owner mismatch.");
});

await RunAsync("raw QR token is not persisted", async () =>
{
    var stored = await db.QrRecipients.AsNoTracking().SingleAsync();
    Assert(stored.TokenHash == qrHash, "Stored QR hash mismatch.");
    Assert(stored.TokenHash.Length == 64, "SHA-256 hex digest must be 64 characters.");
    Assert(!string.Equals(stored.TokenHash, qrToken, StringComparison.Ordinal), "Raw QR token must never be stored.");
});

await RunAsync("inactive identities fail closed", async () =>
{
    var entry = await db.AfWalIdentities.SingleAsync(x => x.AfWalId == afWalId);
    entry.IsActive = false;
    await db.SaveChangesAsync();
    Assert(await afWalDirectory.ResolveOwnerIdAsync(afWalId) is null, "Inactive AfWal ID must not resolve.");
    entry.IsActive = true;
    await db.SaveChangesAsync();
});

await RunAsync("inactive QR tokens fail closed", async () =>
{
    var entry = await db.QrRecipients.SingleAsync(x => x.TokenHash == qrHash);
    entry.IsActive = false;
    await db.SaveChangesAsync();
    Assert(await qrDirectory.ResolveOwnerIdAsync(qrToken) is null, "Inactive QR token must not resolve.");
    entry.IsActive = true;
    await db.SaveChangesAsync();
});

await RunAsync("AfWal ID uniqueness is database-enforced", async () =>
{
    db.AfWalIdentities.Add(new AfWalIdentityEntry
    {
        Id = Guid.NewGuid(),
        OwnerId = Guid.NewGuid(),
        AfWalId = afWalId,
        IsActive = true
    });

    var conflict = false;
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        conflict = true;
    }
    finally
    {
        db.ChangeTracker.Clear();
    }

    Assert(conflict, "Duplicate AfWal ID must be rejected by the database.");
});

await RunAsync("QR hash uniqueness is database-enforced", async () =>
{
    db.QrRecipients.Add(new QrRecipientEntry
    {
        Id = Guid.NewGuid(),
        OwnerId = Guid.NewGuid(),
        TokenHash = qrHash,
        IsActive = true
    });

    var conflict = false;
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        conflict = true;
    }
    finally
    {
        db.ChangeTracker.Clear();
    }

    Assert(conflict, "Duplicate QR token hash must be rejected by the database.");
});

await RunAsync("QR remains opaque and surrounding whitespace is rejected", async () =>
{
    var rejected = false;
    try
    {
        await qrDirectory.ResolveOwnerIdAsync($" {qrToken} ");
    }
    catch (ArgumentException)
    {
        rejected = true;
    }
    Assert(rejected, "QR surrounding whitespace must be rejected.");
});

await RunAsync("directory cancellation is propagated", async () =>
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    var cancelled = false;
    try
    {
        await afWalDirectory.ResolveOwnerIdAsync(afWalId, cts.Token);
    }
    catch (OperationCanceledException)
    {
        cancelled = true;
    }
    Assert(cancelled, "Cancellation must propagate.");
});

Console.WriteLine("AFW-BE-P2P-1 authoritative recipient directory scenarios: PASS");

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
