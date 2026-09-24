using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Domain.Decisions;
using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Infrastructure.Repositories;

public sealed class PaymentRoutingDbContext(DbContextOptions<PaymentRoutingDbContext> options) : DbContext(options)
{
    public DbSet<RoutingDecisionEntity> RoutingDecisions => Set<RoutingDecisionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoutingDecisionEntity>(entity =>
        {
            entity.HasKey(x => x.DecisionId);
            entity.HasIndex(x => x.PaymentIntentId).IsUnique();
            entity.Property(x => x.SelectedRouteJson).IsRequired();
            entity.Property(x => x.AlternativesJson).IsRequired();
            entity.Property(x => x.Reason).IsRequired();
        });
    }
}

public sealed class RoutingDecisionEntity
{
    public Guid DecisionId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string SelectedRouteJson { get; set; } = string.Empty;
    public string AlternativesJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class DurableRoutingDecisionRepository(IDbContextFactory<PaymentRoutingDbContext> factory) : IRoutingDecisionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(RoutingDecision decision, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.RoutingDecisions.Add(new RoutingDecisionEntity
        {
            DecisionId = decision.DecisionId,
            PaymentIntentId = decision.PaymentIntentId,
            SelectedRouteJson = JsonSerializer.Serialize(decision.SelectedRoute, JsonOptions),
            AlternativesJson = JsonSerializer.Serialize(decision.Alternatives, JsonOptions),
            Reason = decision.Reason,
            CreatedAtUtc = decision.CreatedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RoutingDecision?> GetByPaymentIntentAsync(Guid paymentIntentId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.RoutingDecisions.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId, cancellationToken);
        if (entity is null) return null;

        var selected = JsonSerializer.Deserialize<PaymentRoute>(entity.SelectedRouteJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored selected route is invalid.");
        var alternatives = JsonSerializer.Deserialize<PaymentRoute[]>(entity.AlternativesJson, JsonOptions)
            ?? Array.Empty<PaymentRoute>();

        return new RoutingDecision(entity.DecisionId, entity.PaymentIntentId, selected, alternatives, entity.Reason, entity.CreatedAtUtc);
    }
}
