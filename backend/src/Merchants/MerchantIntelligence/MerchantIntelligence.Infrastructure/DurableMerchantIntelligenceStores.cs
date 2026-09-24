using System.Text.Json;
using AfriWallet.Merchants.Intelligence.Application.Abstractions;
using AfriWallet.Merchants.Intelligence.Domain.Findings;
using AfriWallet.Merchants.Intelligence.Domain.Metrics;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Intelligence.Infrastructure;

public sealed class MerchantIntelligenceDbContext(DbContextOptions<MerchantIntelligenceDbContext> options) : DbContext(options)
{
    public DbSet<MerchantRiskFindingEntity> Findings => Set<MerchantRiskFindingEntity>();
    public DbSet<MerchantIntelligenceAuditEntity> AuditEvents => Set<MerchantIntelligenceAuditEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantRiskFindingEntity>(b => { b.HasKey(x => x.FindingId); b.HasIndex(x => new { x.MerchantId, x.CreatedAtUtc }); b.Property(x => x.MerchantId).IsRequired(); b.Property(x => x.MetricsJson).IsRequired(); b.Property(x => x.PatternsJson).IsRequired(); });
        modelBuilder.Entity<MerchantIntelligenceAuditEntity>(b => { b.HasKey(x => x.EventId); b.HasIndex(x => new { x.FindingId, x.OccurredAtUtc }); b.Property(x => x.MetadataJson).IsRequired(); });
    }
}
public sealed class MerchantRiskFindingEntity
{
    public Guid FindingId { get; set; } public string MerchantId { get; set; } = ""; public int Score { get; set; }
    public int Severity { get; set; } public int Recommendation { get; set; } public string MetricsJson { get; set; } = "";
    public string PatternsJson { get; set; } = ""; public DateTimeOffset CreatedAtUtc { get; set; }
}
public sealed class MerchantIntelligenceAuditEntity
{
    public Guid EventId { get; set; } public Guid FindingId { get; set; } public string MerchantId { get; set; } = "";
    public string EventType { get; set; } = ""; public string Actor { get; set; } = ""; public DateTimeOffset OccurredAtUtc { get; set; }
    public string MetadataJson { get; set; } = "";
}
public sealed class EfMerchantIntelligenceRepository(MerchantIntelligenceDbContext db) : IMerchantIntelligenceRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task SaveAsync(MerchantRiskFinding finding, CancellationToken cancellationToken=default)
    {
        var entity = await db.Findings.FindAsync([finding.FindingId], cancellationToken);
        if (entity is null) { entity = new(); db.Findings.Add(entity); }
        entity.FindingId=finding.FindingId; entity.MerchantId=finding.MerchantId; entity.Score=finding.Score; entity.Severity=(int)finding.Severity;
        entity.Recommendation=(int)finding.Recommendation; entity.MetricsJson=JsonSerializer.Serialize(finding.Metrics,Json);
        entity.PatternsJson=JsonSerializer.Serialize(finding.Patterns,Json); entity.CreatedAtUtc=finding.CreatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<MerchantRiskFinding?> GetLatestAsync(string merchantId, CancellationToken cancellationToken=default)
    {
        var rows=await db.Findings.AsNoTracking().Where(x=>x.MerchantId==merchantId.Trim()).ToArrayAsync(cancellationToken);
        var entity=rows.OrderByDescending(x=>x.CreatedAtUtc).FirstOrDefault();
        if(entity is null) return null;
        var metrics=JsonSerializer.Deserialize<MerchantCommerceMetrics>(entity.MetricsJson,Json) ?? throw new InvalidOperationException("Stored metrics are invalid.");
        var patterns=JsonSerializer.Deserialize<MerchantRiskPattern[]>(entity.PatternsJson,Json) ?? [];
        return new(entity.FindingId,entity.MerchantId,entity.Score,(MerchantRiskSeverity)entity.Severity,(MerchantProtectionRecommendation)entity.Recommendation,metrics,patterns,entity.CreatedAtUtc);
    }
}
public sealed class EfMerchantIntelligenceAuditStore(MerchantIntelligenceDbContext db) : IMerchantIntelligenceAuditStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task AppendAsync(MerchantIntelligenceAuditEvent e,CancellationToken cancellationToken=default)
    {
        db.AuditEvents.Add(new(){EventId=e.EventId,FindingId=e.FindingId,MerchantId=e.MerchantId,EventType=e.EventType,Actor=e.Actor,OccurredAtUtc=e.OccurredAtUtc,MetadataJson=JsonSerializer.Serialize(e.Metadata,Json)});
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyCollection<MerchantIntelligenceAuditEvent>> GetAsync(Guid findingId,CancellationToken cancellationToken=default)
    {
        var rows=await db.AuditEvents.AsNoTracking().Where(x=>x.FindingId==findingId).ToArrayAsync(cancellationToken);
        return rows.OrderBy(x=>x.OccurredAtUtc).Select(x=>new MerchantIntelligenceAuditEvent(x.EventId,x.FindingId,x.MerchantId,x.EventType,x.Actor,x.OccurredAtUtc,JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson,Json) ?? new())).ToArray();
    }
}
