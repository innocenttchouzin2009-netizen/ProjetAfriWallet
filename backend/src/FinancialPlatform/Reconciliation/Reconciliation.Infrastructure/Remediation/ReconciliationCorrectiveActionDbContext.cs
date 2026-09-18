using Microsoft.EntityFrameworkCore;

namespace Reconciliation.Infrastructure.Remediation;

public sealed class ReconciliationCorrectiveActionDbContext(
    DbContextOptions<ReconciliationCorrectiveActionDbContext> options)
    : DbContext(options)
{
    public DbSet<ReconciliationCorrectiveActionEntity> Actions => Set<ReconciliationCorrectiveActionEntity>();
    public DbSet<ReconciliationCorrectiveActionAuditEntity> Audit => Set<ReconciliationCorrectiveActionAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var action = modelBuilder.Entity<ReconciliationCorrectiveActionEntity>();
        action.ToTable("ReconciliationCorrectiveActions");
        action.HasKey(x => x.ActionId);
        action.Property(x => x.ResolutionId).IsRequired();
        action.HasIndex(x => x.ResolutionId).IsUnique();
        action.Property(x => x.ReviewId).IsRequired();
        action.Property(x => x.PartnerId).HasMaxLength(128).IsRequired();
        action.Property(x => x.InternalRecordId).HasMaxLength(256);
        action.Property(x => x.ExternalRecordId).HasMaxLength(256);
        action.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
        action.Property(x => x.Description).HasMaxLength(500).IsRequired();
        action.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        action.Property(x => x.CreatedAtUtc).IsRequired();
        action.Property(x => x.Status).IsRequired();
        action.Property(x => x.CompletedBy).HasMaxLength(256);
        action.Property(x => x.CompletionEvidenceReference).HasMaxLength(2048);
        action.Property(x => x.CompletedAtUtc);
        action.Property(x => x.CancelledBy).HasMaxLength(256);
        action.Property(x => x.CancellationReason).HasMaxLength(1024);
        action.Property(x => x.CancelledAtUtc);

        var audit = modelBuilder.Entity<ReconciliationCorrectiveActionAuditEntity>();
        audit.ToTable("ReconciliationCorrectiveActionAudit");
        audit.HasKey(x => x.AuditId);
        audit.Property(x => x.ActionId).IsRequired();
        audit.Property(x => x.ResolutionId).IsRequired();
        audit.Property(x => x.Event).IsRequired();
        audit.Property(x => x.Status).IsRequired();
        audit.Property(x => x.ActorId).HasMaxLength(256).IsRequired();
        audit.Property(x => x.EvidenceReference).HasMaxLength(2048);
        audit.Property(x => x.CancellationReason).HasMaxLength(1024);
        audit.Property(x => x.RecordedAtUtc).IsRequired();
        audit.HasIndex(x => new { x.ResolutionId, x.RecordedAtUtc });
        audit.HasIndex(x => new { x.ActionId, x.RecordedAtUtc });
    }
}

public sealed class ReconciliationCorrectiveActionEntity
{
    public Guid ActionId { get; set; }
    public Guid ResolutionId { get; set; }
    public Guid ReviewId { get; set; }
    public string PartnerId { get; set; } = string.Empty;
    public string? InternalRecordId { get; set; }
    public string? ExternalRecordId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int Status { get; set; }
    public string? CompletedBy { get; set; }
    public string? CompletionEvidenceReference { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}

public sealed class ReconciliationCorrectiveActionAuditEntity
{
    public Guid AuditId { get; set; }
    public Guid ActionId { get; set; }
    public Guid ResolutionId { get; set; }
    public int Event { get; set; }
    public int Status { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
