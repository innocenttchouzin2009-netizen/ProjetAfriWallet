using Microsoft.EntityFrameworkCore;

namespace Reconciliation.Infrastructure.Resolutions;

public sealed class ReconciliationResolutionDbContext(DbContextOptions<ReconciliationResolutionDbContext> options)
    : DbContext(options)
{
    public DbSet<ReconciliationResolutionEntity> Resolutions => Set<ReconciliationResolutionEntity>();
    public DbSet<ReconciliationResolutionAuditEntity> Audit => Set<ReconciliationResolutionAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var resolution = modelBuilder.Entity<ReconciliationResolutionEntity>();
        resolution.ToTable("ReconciliationResolutions");
        resolution.HasKey(x => x.ResolutionId);
        resolution.Property(x => x.ReviewId).IsRequired();
        resolution.HasIndex(x => x.ReviewId).IsUnique();
        resolution.Property(x => x.PartnerId).HasMaxLength(128).IsRequired();
        resolution.Property(x => x.InternalRecordId).HasMaxLength(256);
        resolution.Property(x => x.ExternalRecordId).HasMaxLength(256);
        resolution.Property(x => x.Disposition).IsRequired();
        resolution.Property(x => x.Rationale).HasMaxLength(2048).IsRequired();
        resolution.Property(x => x.EvidenceReference).HasMaxLength(2048).IsRequired();
        resolution.Property(x => x.ResolvedBy).HasMaxLength(256).IsRequired();
        resolution.Property(x => x.ResolvedAtUtc).IsRequired();

        var audit = modelBuilder.Entity<ReconciliationResolutionAuditEntity>();
        audit.ToTable("ReconciliationResolutionAudit");
        audit.HasKey(x => x.AuditId);
        audit.Property(x => x.ResolutionId).IsRequired();
        audit.Property(x => x.ReviewId).IsRequired();
        audit.Property(x => x.Disposition).IsRequired();
        audit.Property(x => x.ResolvedBy).HasMaxLength(256).IsRequired();
        audit.Property(x => x.Rationale).HasMaxLength(2048).IsRequired();
        audit.Property(x => x.EvidenceReference).HasMaxLength(2048).IsRequired();
        audit.Property(x => x.RecordedAtUtc).IsRequired();
        audit.HasIndex(x => new { x.ReviewId, x.RecordedAtUtc });
        audit.HasIndex(x => x.ResolutionId).IsUnique();
    }
}

public sealed class ReconciliationResolutionEntity
{
    public Guid ResolutionId { get; set; }
    public Guid ReviewId { get; set; }
    public string PartnerId { get; set; } = string.Empty;
    public string? InternalRecordId { get; set; }
    public string? ExternalRecordId { get; set; }
    public int Disposition { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public string ResolvedBy { get; set; } = string.Empty;
    public DateTime ResolvedAtUtc { get; set; }
}

public sealed class ReconciliationResolutionAuditEntity
{
    public Guid AuditId { get; set; }
    public Guid ResolutionId { get; set; }
    public Guid ReviewId { get; set; }
    public int Disposition { get; set; }
    public string ResolvedBy { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; }
}
