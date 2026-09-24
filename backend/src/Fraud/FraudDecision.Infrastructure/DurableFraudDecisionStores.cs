using System.Text.Json;
using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Domain.Decisions;
using AfriWallet.Fraud.Decision.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Fraud.Decision.Infrastructure;

public sealed class FraudDecisionDbContext(DbContextOptions<FraudDecisionDbContext> options) : DbContext(options)
{
    public DbSet<FraudDecisionEntity> Decisions => Set<FraudDecisionEntity>();
    public DbSet<FraudDecisionAuditEntity> AuditEvents => Set<FraudDecisionAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FraudDecisionEntity>(b =>
        {
            b.HasKey(x => x.DecisionId);
            b.HasIndex(x => x.TransactionId).IsUnique();
            b.Property(x => x.Awid).IsRequired();
            b.Property(x => x.DeviceId).IsRequired();
            b.Property(x => x.EvaluationsJson).IsRequired();
        });
        modelBuilder.Entity<FraudDecisionAuditEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.DecisionId, x.OccurredAtUtc });
            b.Property(x => x.Awid).IsRequired();
            b.Property(x => x.Action).IsRequired();
            b.Property(x => x.Actor).IsRequired();
            b.Property(x => x.MetadataJson).IsRequired();
        });
    }
}

public sealed class FraudDecisionEntity
{
    public Guid DecisionId { get; set; }
    public Guid TransactionId { get; set; }
    public string Awid { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public int Score { get; set; }
    public int Band { get; set; }
    public int Action { get; set; }
    public string EvaluationsJson { get; set; } = "";
    public DateTimeOffset DecidedAtUtc { get; set; }
}

public sealed class FraudDecisionAuditEntity
{
    public Guid Id { get; set; }
    public Guid DecisionId { get; set; }
    public Guid TransactionId { get; set; }
    public string Awid { get; set; } = "";
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string MetadataJson { get; set; } = "";
}

public sealed class EfFraudDecisionRepository(FraudDecisionDbContext db) : IFraudDecisionRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(FraudDecision decision, CancellationToken cancellationToken = default)
    {
        var entity = await db.Decisions.SingleOrDefaultAsync(x => x.TransactionId == decision.TransactionId, cancellationToken);
        if (entity is null)
        {
            entity = new FraudDecisionEntity();
            db.Decisions.Add(entity);
        }

        entity.DecisionId = decision.DecisionId;
        entity.TransactionId = decision.TransactionId;
        entity.Awid = decision.Awid;
        entity.DeviceId = decision.DeviceId;
        entity.Score = decision.Score;
        entity.Band = (int)decision.Band;
        entity.Action = (int)decision.Action;
        entity.EvaluationsJson = JsonSerializer.Serialize(decision.Evaluations, Json);
        entity.DecidedAtUtc = decision.DecidedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FraudDecision?> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var entity = await db.Decisions.AsNoTracking().SingleOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);
        if (entity is null) return null;
        var evaluations = JsonSerializer.Deserialize<FraudRuleEvaluation[]>(entity.EvaluationsJson, Json) ?? [];
        return new FraudDecision(entity.DecisionId, entity.TransactionId, entity.Awid, entity.DeviceId, entity.Score,
            (FraudDecisionBand)entity.Band, (FraudDecisionAction)entity.Action, evaluations, entity.DecidedAtUtc);
    }
}

public sealed class EfFraudDecisionAuditStore(FraudDecisionDbContext db) : IFraudDecisionAuditStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task AppendAsync(FraudDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.AuditEvents.Add(new FraudDecisionAuditEntity
        {
            Id = auditEvent.Id,
            DecisionId = auditEvent.DecisionId,
            TransactionId = auditEvent.TransactionId,
            Awid = auditEvent.Awid,
            Action = auditEvent.Action,
            Actor = auditEvent.Actor,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            MetadataJson = JsonSerializer.Serialize(auditEvent.Metadata, Json)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FraudDecisionAuditEvent>> GetByDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        var rows = await db.AuditEvents.AsNoTracking().Where(x => x.DecisionId == decisionId).ToArrayAsync(cancellationToken);
        return rows.OrderBy(x => x.OccurredAtUtc).Select(x => new FraudDecisionAuditEvent(x.Id, x.DecisionId, x.TransactionId,
            x.Awid, x.Action, x.Actor, x.OccurredAtUtc,
            JsonSerializer.Deserialize<Dictionary<string, string>>(x.MetadataJson, Json) ?? new())).ToArray();
    }
}