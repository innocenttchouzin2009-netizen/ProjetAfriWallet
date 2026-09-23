using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Payout.Infrastructure;

public sealed class MerchantPayoutDbContext(DbContextOptions<MerchantPayoutDbContext> options) : DbContext(options)
{
    public DbSet<MerchantReceivableEntity> Receivables => Set<MerchantReceivableEntity>();
    public DbSet<MerchantPayoutDestinationEntity> Destinations => Set<MerchantPayoutDestinationEntity>();
    public DbSet<MerchantPayoutExecutionEntity> Payouts => Set<MerchantPayoutExecutionEntity>();
    public DbSet<MerchantPayoutAuditEntity> Audit => Set<MerchantPayoutAuditEntity>();
    public DbSet<MerchantPayoutProviderResultEntity> ProviderResults => Set<MerchantPayoutProviderResultEntity>();
    public DbSet<MerchantPayoutReconciliationEntity> Reconciliations => Set<MerchantPayoutReconciliationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<MerchantReceivableEntity>(entity =>
        {
            entity.ToTable("MerchantReceivables");
            entity.HasKey(x => x.ReceivableId);
            entity.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.CaptureStatus).HasMaxLength(32).IsRequired();
        });

        builder.Entity<MerchantPayoutDestinationEntity>(entity =>
        {
            entity.ToTable("MerchantPayoutDestinations");
            entity.HasKey(x => x.DestinationId);
            entity.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.HasIndex(x => new { x.MerchantId, x.Active });
        });

        builder.Entity<MerchantPayoutExecutionEntity>(entity =>
        {
            entity.ToTable("MerchantPayoutExecutions");
            entity.HasKey(x => x.PayoutId);
            entity.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.ReceivableId).IsUnique();
        });

        builder.Entity<MerchantPayoutAuditEntity>(entity =>
        {
            entity.ToTable("MerchantPayoutAudit");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Actor).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.PayoutId, x.OccurredAtUtc });
        });

        builder.Entity<MerchantPayoutProviderResultEntity>(entity =>
        {
            entity.ToTable("MerchantPayoutProviderResults");
            entity.HasKey(x => x.ResultId);
            entity.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ProviderReference).HasMaxLength(256);
            entity.Property(x => x.FailureCode).HasMaxLength(128);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.HasIndex(x => new { x.PayoutId, x.ObservedAtUtc });
        });

        builder.Entity<MerchantPayoutReconciliationEntity>(entity =>
        {
            entity.ToTable("MerchantPayoutReconciliations");
            entity.HasKey(x => x.ReconciliationId);
            entity.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ReasonCode).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => new { x.PayoutId, x.EvaluatedAtUtc });
            entity.HasIndex(x => x.ResultId).IsUnique();
        });
    }
}

public sealed class MerchantReceivableEntity
{
    public Guid ReceivableId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CaptureStatus { get; set; } = string.Empty;
    public bool SettlementReady { get; set; }
    public string? CaptureProviderReference { get; set; }
}

public sealed class MerchantPayoutDestinationEntity
{
    public Guid DestinationId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public int Type { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MerchantPayoutExecutionEntity
{
    public Guid PayoutId { get; set; }
    public Guid ReceivableId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid DestinationId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MerchantPayoutAuditEntity
{
    public Guid EventId { get; set; }
    public Guid PayoutId { get; set; }
    public Guid ReceivableId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
}


public sealed class MerchantPayoutProviderResultEntity
{
    public Guid ResultId { get; set; }
    public Guid PayoutId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public int Status { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureCode { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
}

public sealed class MerchantPayoutReconciliationEntity
{
    public Guid ReconciliationId { get; set; }
    public Guid PayoutId { get; set; }
    public Guid ResultId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public int Status { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
}
