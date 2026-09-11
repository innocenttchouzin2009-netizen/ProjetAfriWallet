using Microsoft.EntityFrameworkCore;

namespace AfriWallet.P2P.Directory.Persistence;

public sealed class RecipientDirectoryDbContext(DbContextOptions<RecipientDirectoryDbContext> options) : DbContext(options)
{
    public DbSet<AfWalIdentityEntry> AfWalIdentities => Set<AfWalIdentityEntry>();
    public DbSet<QrRecipientEntry> QrRecipients => Set<QrRecipientEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var afWal = modelBuilder.Entity<AfWalIdentityEntry>();
        afWal.ToTable("P2PAfWalIdentities");
        afWal.HasKey(x => x.Id);
        afWal.Property(x => x.AfWalId).HasMaxLength(128).IsRequired();
        afWal.HasIndex(x => x.AfWalId).IsUnique();
        afWal.Property(x => x.OwnerId).IsRequired();
        afWal.Property(x => x.IsActive).IsRequired();

        var qr = modelBuilder.Entity<QrRecipientEntry>();
        qr.ToTable("P2PQrRecipients");
        qr.HasKey(x => x.Id);
        qr.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        qr.HasIndex(x => x.TokenHash).IsUnique();
        qr.Property(x => x.OwnerId).IsRequired();
        qr.Property(x => x.IsActive).IsRequired();
    }
}

public sealed class AfWalIdentityEntry
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string AfWalId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class QrRecipientEntry
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
