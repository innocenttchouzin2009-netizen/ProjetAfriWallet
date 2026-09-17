using Microsoft.EntityFrameworkCore;

namespace Reconciliation.Infrastructure.Repositories;

public sealed class ReconciliationReviewDbContext(DbContextOptions<ReconciliationReviewDbContext> options) : DbContext(options)
{
    public DbSet<ReconciliationReviewEntity> ReviewItems => Set<ReconciliationReviewEntity>();
    public DbSet<ReconciliationReviewAuditEntity> ReviewAudit => Set<ReconciliationReviewAuditEntity>();
    public DbSet<ReviewResolutionEntity> ReviewResolutions => Set<ReviewResolutionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var review = modelBuilder.Entity<ReconciliationReviewEntity>();
        review.ToTable("ReconciliationReviewItems");
        review.HasKey(x => x.ReviewId);
        review.Property(x => x.PartnerId).HasMaxLength(128).IsRequired();
        review.Property(x => x.InternalRecordId).HasMaxLength(256);
        review.Property(x => x.ExternalRecordId).HasMaxLength(256);
        review.Property(x => x.MatchType).IsRequired();
        review.Property(x => x.ConfidenceScore).IsRequired();
        review.Property(x => x.QueuedAtUtc).IsRequired();
        review.Property(x => x.Status).IsRequired();
        review.Property(x => x.ReviewerId).HasMaxLength(256);
        review.Property(x => x.DecisionReason).HasMaxLength(1024);
        review.HasIndex(x => new { x.PartnerId, x.Status, x.QueuedAtUtc });

        var audit = modelBuilder.Entity<ReconciliationReviewAuditEntity>();
        audit.ToTable("ReconciliationReviewAudit");
        audit.HasKey(x => x.AuditId);
        audit.Property(x => x.ReviewId).IsRequired();
        audit.Property(x => x.PreviousStatus).IsRequired();
        audit.Property(x => x.NewStatus).IsRequired();
        audit.Property(x => x.ReviewerId).HasMaxLength(256).IsRequired();
        audit.Property(x => x.Reason).HasMaxLength(1024);
        audit.Property(x => x.DecidedAtUtc).IsRequired();
        audit.HasIndex(x => new { x.ReviewId, x.DecidedAtUtc });

        var resolution = modelBuilder.Entity<ReviewResolutionEntity>();
        resolution.ToTable("ReconciliationReviewResolutions");
        resolution.HasKey(x => x.ReviewId);
        resolution.Property(x => x.PartnerId).HasMaxLength(128).IsRequired();
        resolution.Property(x => x.EvidenceId).HasMaxLength(256).IsRequired();
        resolution.Property(x => x.ResolvedBy).HasMaxLength(256).IsRequired();
        resolution.Property(x => x.ResolvedAtUtc).IsRequired();
        resolution.HasIndex(x => x.EvidenceId);
    }
}

public sealed class ReconciliationReviewEntity
{
    public Guid ReviewId { get; set; }
    public string PartnerId { get; set; } = string.Empty;
    public string? InternalRecordId { get; set; }
    public string? ExternalRecordId { get; set; }
    public int MatchType { get; set; }
    public int ConfidenceScore { get; set; }
    public long? AmountDifferenceMinor { get; set; }
    public long? TimeDifferenceTicks { get; set; }
    public DateTime QueuedAtUtc { get; set; }
    public int Status { get; set; }
    public string? ReviewerId { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}

public sealed class ReconciliationReviewAuditEntity
{
    public Guid AuditId { get; set; }
    public Guid ReviewId { get; set; }
    public int PreviousStatus { get; set; }
    public int NewStatus { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime DecidedAtUtc { get; set; }
}

public sealed class ReviewResolutionEntity
{
    public Guid ReviewId { get; set; }
    public string PartnerId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string ResolvedBy { get; set; } = string.Empty;
    public DateTime ResolvedAtUtc { get; set; }
}
