using Microsoft.EntityFrameworkCore;

namespace Settlement.Infrastructure.Persistence;

public sealed class SettlementDbContext(DbContextOptions<SettlementDbContext> options) : DbContext(options)
{
    public DbSet<SettlementInstructionEntity> Instructions => Set<SettlementInstructionEntity>();
    public DbSet<SettlementBatchEntity> Batches => Set<SettlementBatchEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SettlementInstructionEntity>(entity =>
        {
            entity.ToTable("SettlementInstructions");
            entity.HasKey(x => x.InstructionId);
            entity.Property(x => x.SourceCurrency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.DestinationCurrency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<SettlementBatchEntity>(entity =>
        {
            entity.ToTable("SettlementBatches");
            entity.HasKey(x => x.BatchId);
            entity.Property(x => x.SourceCurrency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.DestinationCurrency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.InstructionIdsCsv).IsRequired();
        });
    }
}

public sealed class SettlementInstructionEntity
{
    public Guid InstructionId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public string SourceCurrency { get; set; } = string.Empty;
    public string DestinationCurrency { get; set; } = string.Empty;
    public long SourceAmountMinor { get; set; }
    public long DestinationAmountMinor { get; set; }
    public decimal? QuoteRate { get; set; }
    public DateTime? QuoteAtUtc { get; set; }
    public DateTime? QuoteExpiresAtUtc { get; set; }
    public int Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
}

public sealed class SettlementBatchEntity
{
    public Guid BatchId { get; set; }
    public string InstructionIdsCsv { get; set; } = string.Empty;
    public string SourceCurrency { get; set; } = string.Empty;
    public string DestinationCurrency { get; set; } = string.Empty;
    public long TotalSourceAmountMinor { get; set; }
    public long TotalDestinationAmountMinor { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
}
