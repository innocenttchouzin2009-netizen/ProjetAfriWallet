using Microsoft.EntityFrameworkCore;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Application.Scoring;
using PaymentRouting.Application.Services;
using PaymentRouting.Domain.Decisions;
using PaymentRouting.Domain.Providers;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Persistence;
using PaymentRouting.Infrastructure.Providers;
using PaymentRouting.Infrastructure.Repositories;

static void Check(string name, bool condition)
{
    if (!condition) throw new InvalidOperationException($"Scenario failed: {name}");
    Console.WriteLine($"{name} PASS");
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afwal-payment-routing-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={dbPath}";
var intentId = Guid.NewGuid();
Guid decisionId;

try
{
    await using (var db = new PaymentRoutingDbContext(
        new DbContextOptionsBuilder<PaymentRoutingDbContext>()
            .UseSqlite(connectionString)
            .Options))
    {
        await db.Database.EnsureCreatedAsync();

        var providers = new EfPaymentProviderRepository(db);
        await SandboxProviderBootstrap.SeedAsync(providers, CancellationToken.None);
        await SandboxProviderBootstrap.SeedAsync(providers, CancellationToken.None);

        Check("idempotent provider bootstrap", (await providers.ListAsync(CancellationToken.None)).Count == 5);

        var orange = await providers.GetAsync("ORANGE-MONEY-CM", CancellationToken.None)
            ?? throw new InvalidOperationException("Orange Money provider missing.");
        orange.UpdateHealth(ProviderStatus.Degraded, 0.93, 420);
        await providers.UpdateAsync(orange, CancellationToken.None);

        var decisions = new EfRoutingDecisionRepository(db);
        var service = new PaymentRoutingService(providers, decisions, new PaymentRouteScorer());

        var decision = await service.RouteAsync(
            new RoutingRequest(
                intentId,
                "CM",
                "XAF",
                125_000,
                PaymentRail.MobileMoney,
                null,
                "corr-durable-001"),
            policy: null,
            CancellationToken.None);

        decisionId = decision.DecisionId;
        Check("initial durable route", decision.PaymentIntentId == intentId);
        Check("decision persisted", await decisions.GetByPaymentIntentAsync(intentId, CancellationToken.None) is not null);
    }

    await using (var restartedDb = new PaymentRoutingDbContext(
        new DbContextOptionsBuilder<PaymentRoutingDbContext>()
            .UseSqlite(connectionString)
            .Options))
    {
        var providers = new EfPaymentProviderRepository(restartedDb);
        var restoredOrange = await providers.GetAsync("ORANGE-MONEY-CM", CancellationToken.None);

        Check("provider health survives restart",
            restoredOrange is not null &&
            restoredOrange.Status == ProviderStatus.Degraded &&
            Math.Abs(restoredOrange.SuccessRate - 0.93) < 0.0001 &&
            Math.Abs(restoredOrange.AverageLatencyMs - 420) < 0.0001);

        var decisions = new EfRoutingDecisionRepository(restartedDb);
        var service = new PaymentRoutingService(providers, decisions, new PaymentRouteScorer());

        var replay = await service.RouteAsync(
            new RoutingRequest(
                intentId,
                "CM",
                "XAF",
                125_000,
                PaymentRail.MobileMoney,
                "MTN-MOMO-CM",
                "corr-durable-replay"),
            policy: null,
            CancellationToken.None);

        Check("restart-safe routing idempotency", replay.DecisionId == decisionId);

        var conflict = false;
        try
        {
            await decisions.AddAsync(
                new RoutingDecision(
                    Guid.NewGuid(),
                    intentId,
                    replay.SelectedRoute,
                    replay.Alternatives,
                    "conflicting duplicate",
                    DateTime.UtcNow),
                CancellationToken.None);
        }
        catch (RoutingDecisionConflictException)
        {
            conflict = true;
        }

        Check("unique payment intent constraint", conflict);
        Check("single durable decision",
            await restartedDb.Decisions.CountAsync(x => x.PaymentIntentId == intentId) == 1);
    }

    Console.WriteLine("AFW-BE-PAYMENT-ROUTING-1 durable routing scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
