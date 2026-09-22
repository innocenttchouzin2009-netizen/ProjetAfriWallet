using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Capture.Infrastructure;

public sealed class MerchantCaptureDbContext(DbContextOptions<MerchantCaptureDbContext> options):DbContext(options)
{
    public DbSet<MerchantCaptureExecutionEntity> Executions=>Set<MerchantCaptureExecutionEntity>();
    public DbSet<MerchantCaptureAuditEntity> Audit=>Set<MerchantCaptureAuditEntity>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<MerchantCaptureExecutionEntity>(e=>{e.ToTable("MerchantCaptureExecutions");e.HasKey(x=>x.ExecutionId);e.Property(x=>x.MerchantId).HasMaxLength(128).IsRequired();e.Property(x=>x.Currency).HasMaxLength(3).IsRequired();e.Property(x=>x.IdempotencyKey).HasMaxLength(128).IsRequired();e.HasIndex(x=>x.IdempotencyKey).IsUnique();e.HasIndex(x=>x.DecisionId).IsUnique();});
        b.Entity<MerchantCaptureAuditEntity>(e=>{e.ToTable("MerchantCaptureAudit");e.HasKey(x=>x.EventId);e.Property(x=>x.EventType).HasMaxLength(64).IsRequired();e.Property(x=>x.Actor).HasMaxLength(256).IsRequired();e.HasIndex(x=>new{x.ExecutionId,x.OccurredAtUtc});});
    }
}
public sealed class MerchantCaptureExecutionEntity
{
    public Guid ExecutionId{get;set;} public Guid DecisionId{get;set;} public Guid PaymentIntentId{get;set;} public string MerchantId{get;set;}=string.Empty; public long AmountMinor{get;set;} public string Currency{get;set;}=string.Empty; public string IdempotencyKey{get;set;}=string.Empty; public int Status{get;set;} public string? ProviderReference{get;set;} public string? FailureCode{get;set;} public DateTimeOffset CreatedAtUtc{get;set;} public DateTimeOffset UpdatedAtUtc{get;set;}
}
public sealed class MerchantCaptureAuditEntity
{
    public Guid EventId{get;set;} public Guid ExecutionId{get;set;} public Guid DecisionId{get;set;} public Guid PaymentIntentId{get;set;} public string MerchantId{get;set;}=string.Empty; public string EventType{get;set;}=string.Empty; public string Actor{get;set;}=string.Empty; public DateTimeOffset OccurredAtUtc{get;set;} public string MetadataJson{get;set;}="{}";
}
