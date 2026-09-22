using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Domain.Decisions;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Persistence;

namespace PaymentRouting.Infrastructure.Repositories;

public sealed class EfRoutingDecisionRepository(PaymentRoutingDbContext dbContext)
    : IRoutingDecisionRepository
{
    public async Task AddAsync(RoutingDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);

        dbContext.Decisions.Add(ToEntity(decision));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.Entry(dbContext.Decisions.Local.Single(x => x.DecisionId == decision.DecisionId))
                .State = EntityState.Detached;

            if (await dbContext.Decisions.AsNoTracking()
                .AnyAsync(x => x.PaymentIntentId == decision.PaymentIntentId, cancellationToken))
            {
                throw new RoutingDecisionConflictException(decision.PaymentIntentId);
            }

            throw new InvalidOperationException("Routing decision could not be persisted.", exception);
        }
    }

    public async Task<RoutingDecision?> GetByPaymentIntentAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Decisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    private static RoutingDecisionEntity ToEntity(RoutingDecision value) => new()
    {
        DecisionId = value.DecisionId,
        PaymentIntentId = value.PaymentIntentId,
        SelectedProviderId = value.SelectedRoute.ProviderId,
        SelectedRail = (int)value.SelectedRoute.Rail,
        SelectedScore = value.SelectedRoute.Score,
        SelectedCostScore = value.SelectedRoute.CostScore,
        SelectedSuccessRate = value.SelectedRoute.SuccessRate,
        SelectedAverageLatencyMs = value.SelectedRoute.AverageLatencyMs,
        SelectedPriority = value.SelectedRoute.Priority,
        AlternativesJson = JsonSerializer.Serialize(value.Alternatives),
        Reason = value.Reason,
        CreatedAtUtc = DateTime.SpecifyKind(value.CreatedAtUtc, DateTimeKind.Utc)
    };

    private static RoutingDecision ToDomain(RoutingDecisionEntity value)
    {
        var selected = new PaymentRoute(
            value.SelectedProviderId,
            (PaymentRail)value.SelectedRail,
            value.SelectedScore,
            value.SelectedCostScore,
            value.SelectedSuccessRate,
            value.SelectedAverageLatencyMs,
            value.SelectedPriority,
            false);

        var alternatives = JsonSerializer.Deserialize<PaymentRoute[]>(value.AlternativesJson)
            ?? Array.Empty<PaymentRoute>();

        return new RoutingDecision(
            value.DecisionId,
            value.PaymentIntentId,
            selected,
            alternatives,
            value.Reason,
            DateTime.SpecifyKind(value.CreatedAtUtc, DateTimeKind.Utc));
    }
}
