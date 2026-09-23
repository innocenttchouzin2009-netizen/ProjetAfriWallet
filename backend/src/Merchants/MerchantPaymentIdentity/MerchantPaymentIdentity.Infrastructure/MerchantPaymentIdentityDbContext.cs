using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.PaymentIdentity.Infrastructure;

public sealed class MerchantPaymentIdentityDbContext(DbContextOptions<MerchantPaymentIdentityDbContext> options) : DbContext(options)
{
    public DbSet<MerchantPaymentIdentityEntity> Identities => Set<MerchantPaymentIdentityEntity>();
    public DbSet<MerchantPaymentIdentityAuditEntity> Audit => Set<MerchantPaymentIdentityAuditEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<MerchantPaymentIdentityEntity>(entity =>
        {
            entity.ToTable("MerchantPaymentIdentities");
            entity.HasKey(x => x.IdentityId);
            entity.Property(x => x.MerchantAfWalId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.MerchantId).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.MerchantAfWalId).IsUnique();
            entity.HasIndex(x => x.MerchantId).IsUnique();
            entity.HasIndex(x => new { x.WalletId, x.Status });
        });

        builder.Entity<MerchantPaymentIdentityAuditEntity>(entity =>
        {
            entity.ToTable("MerchantPaymentIdentityAudit");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.MerchantAfWalId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.MerchantId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Actor).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(512);
            entity.HasIndex(x => new { x.IdentityId, x.OccurredAtUtc });
        });
    }
}

public sealed class MerchantPaymentIdentityEntity
{
    public Guid IdentityId { get; set; }
    public string MerchantAfWalId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public Guid WalletId { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MerchantPaymentIdentityAuditEntity
{
    public Guid EventId { get; set; }
    public Guid IdentityId { get; set; }
    public string MerchantAfWalId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public Guid WalletId { get; set; }
    public int Operation { get; set; }
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? Detail { get; set; }
}
