using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Settlement.Infrastructure;

public sealed class MerchantSettlementDbContext(DbContextOptions<MerchantSettlementDbContext> options) : DbContext(options)
{
    public DbSet<MerchantCaptureEntity> Captures => Set<MerchantCaptureEntity>();
    public DbSet<MerchantSettlementEntity> Settlements => Set<MerchantSettlementEntity>();
    public DbSet<MerchantSettlementAuditEntity> AuditEvents => Set<MerchantSettlementAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantCaptureEntity>(e =>
        {
            e.ToTable("MerchantCaptures");
            e.HasKey(x => x.DecisionId);
            e.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        });

        modelBuilder.Entity<MerchantSettlementEntity>(e =>
        {
            e.ToTable("MerchantSettlements");
            e.HasKey(x => x.SettlementId);
            e.HasIndex(x => x.PaymentDecisionId).IsUnique();
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<MerchantSettlementAuditEntity>(e =>
        {
            e.ToTable("MerchantSettlementAudit");
            e.HasKey(x => x.EventId);
            e.HasIndex(x => new { x.SettlementId, x.OccurredAtUtc });
            e.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(256).IsRequired();
        });
    }
}

public sealed class MerchantCaptureEntity
{
    public Guid DecisionId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string DecisionType { get; set; } = string.Empty;
    public string DecisionStatus { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string MerchantRegistryStatus { get; set; } = string.Empty;
    public string MerchantVerificationStatus { get; set; } = string.Empty;
}

public sealed class MerchantSettlementEntity
{
    public Guid SettlementId { get; set; }
    public Guid PaymentDecisionId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public int Route { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int Status { get; set; }
    public int ReasonCode { get; set; }
    public string? CorrelationId { get; set; }
    public string? ProviderReference { get; set; }
    public string AttemptsJson { get; set; } = "[]";
    public string CompensationsJson { get; set; } = "[]";
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public string? CompletedAtUtc { get; set; }
}

public sealed class MerchantSettlementAuditEntity
{
    public Guid EventId { get; set; }
    public Guid SettlementId { get; set; }
    public Guid PaymentDecisionId { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string OccurredAtUtc { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}
