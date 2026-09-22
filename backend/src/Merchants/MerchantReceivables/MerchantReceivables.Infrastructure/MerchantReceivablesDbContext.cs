using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Receivables.Infrastructure;

public sealed class MerchantReceivablesDbContext(DbContextOptions<MerchantReceivablesDbContext> options) : DbContext(options)
{
    public DbSet<MerchantReceivableEntity> Receivables => Set<MerchantReceivableEntity>();
    public DbSet<MerchantFeeScheduleEntity> FeeSchedules => Set<MerchantFeeScheduleEntity>();
    public DbSet<MerchantReceivableAuditEntity> Audit => Set<MerchantReceivableAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantReceivableEntity>(e =>
        {
            e.ToTable("MerchantReceivables");
            e.HasKey(x => x.ReceivableId);
            e.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.CaptureExecutionId).IsUnique();
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.MerchantId, x.Currency, x.Status });
        });

        modelBuilder.Entity<MerchantFeeScheduleEntity>(e =>
        {
            e.ToTable("MerchantFeeSchedules");
            e.HasKey(x => new { x.MerchantId, x.Currency });
            e.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        });

        modelBuilder.Entity<MerchantReceivableAuditEntity>(e =>
        {
            e.ToTable("MerchantReceivableAudit");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(256).IsRequired();
            e.HasIndex(x => new { x.ReceivableId, x.OccurredAtUtc });
        });
    }
}

public sealed class MerchantReceivableEntity
{
    public Guid ReceivableId { get; set; }
    public Guid CaptureExecutionId { get; set; }
    public Guid DecisionId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public long GrossAmountMinor { get; set; }
    public long FeeAmountMinor { get; set; }
    public long NetAmountMinor { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int Status { get; set; }
    public Guid? SettlementId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }
}

public sealed class MerchantFeeScheduleEntity
{
    public string MerchantId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public int PercentageBasisPoints { get; set; }
    public long FixedFeeMinor { get; set; }
}

public sealed class MerchantReceivableAuditEntity
{
    public Guid EventId { get; set; }
    public Guid ReceivableId { get; set; }
    public Guid CaptureExecutionId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
