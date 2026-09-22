using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Settlement.Infrastructure.Persistence;

public sealed class MerchantSettlementDbContext(DbContextOptions<MerchantSettlementDbContext> options) : DbContext(options)
{
    public DbSet<MerchantSettlementEntity> Settlements => Set<MerchantSettlementEntity>();
    public DbSet<MerchantSettlementAttemptEntity> Attempts => Set<MerchantSettlementAttemptEntity>();
    public DbSet<MerchantSettlementCompensationEntity> Compensations => Set<MerchantSettlementCompensationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantSettlementEntity>(entity =>
        {
            entity.ToTable("MerchantSettlements");
            entity.HasKey(x => x.SettlementId);
            entity.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.PaymentDecisionId).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<MerchantSettlementAttemptEntity>(entity =>
        {
            entity.ToTable("MerchantSettlementAttempts");
            entity.HasKey(x => x.AttemptId);
            entity.HasIndex(x => new { x.SettlementId, x.AttemptNumber }).IsUnique();
        });

        modelBuilder.Entity<MerchantSettlementCompensationEntity>(entity =>
        {
            entity.ToTable("MerchantSettlementCompensations");
            entity.HasKey(x => x.CompensationId);
            entity.HasIndex(x => x.SettlementId);
        });
    }
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
    public Guid? CoreSettlementInstructionId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class MerchantSettlementAttemptEntity
{
    public Guid AttemptId { get; set; }
    public Guid SettlementId { get; set; }
    public int AttemptNumber { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string Result { get; set; } = string.Empty;
    public DateTimeOffset AttemptedAtUtc { get; set; }
}

public sealed class MerchantSettlementCompensationEntity
{
    public Guid CompensationId { get; set; }
    public Guid SettlementId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
