using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Billing.Infrastructure;

public sealed class MerchantBillingDbContext(DbContextOptions<MerchantBillingDbContext> options) : DbContext(options)
{
    public DbSet<MerchantBillingHandoffEntity> Handoffs => Set<MerchantBillingHandoffEntity>();
    public DbSet<MerchantBillingAuditEntity> Audit => Set<MerchantBillingAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantBillingHandoffEntity>(e =>
        {
            e.ToTable("MerchantBillingHandoffs");
            e.HasKey(x => x.HandoffId);
            e.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            e.Property(x => x.LastError).HasMaxLength(1024);
            e.HasIndex(x => x.CaptureExecutionId).IsUnique();
            e.HasIndex(x => x.ReceivableId).IsUnique();
        });

        modelBuilder.Entity<MerchantBillingAuditEntity>(e =>
        {
            e.ToTable("MerchantBillingAudit");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(256).IsRequired();
            e.HasIndex(x => new { x.HandoffId, x.OccurredAtUtc });
        });
    }
}

public sealed class MerchantBillingHandoffEntity
{
    public Guid HandoffId { get; set; }
    public Guid CaptureExecutionId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public int Status { get; set; }
    public Guid? ReceivableId { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MerchantBillingAuditEntity
{
    public Guid EventId { get; set; }
    public Guid HandoffId { get; set; }
    public Guid CaptureExecutionId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
