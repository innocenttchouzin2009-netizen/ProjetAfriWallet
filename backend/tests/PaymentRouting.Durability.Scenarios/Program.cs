using Microsoft.EntityFrameworkCore;
using PaymentRouting.Domain.Decisions;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Repositories;

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-routing-{Guid.NewGuid():N}.db");
try
{
    var options = new DbContextOptionsBuilder<PaymentRoutingDbContext>().UseSqlite($"Data Source={dbPath}").Options;
    await using (var db = new PaymentRoutingDbContext(options)) await db.Database.EnsureCreatedAsync();

    IDbContextFactory<PaymentRoutingDbContext> Factory() = new TestFactory(options);
    var paymentIntentId = Guid.NewGuid();
    var selected = new PaymentRoute("provider-a", PaymentRail.MobileMoney, 1.25m, 0.2m, 0.99, 120, 1, false);
    var decision = new RoutingDecision(Guid.NewGuid(), paymentIntentId, selected, Array.Empty<PaymentRoute>(), "best-route", DateTime.UtcNow);

    await new DurableRoutingDecisionRepository(Factory()).AddAsync(decision, CancellationToken.None);
    var restored = await new DurableRoutingDecisionRepository(Factory()).GetByPaymentIntentAsync(paymentIntentId, CancellationToken.None);

    if (restored is null || restored.DecisionId != decision.DecisionId || restored.SelectedRoute.ProviderId != "provider-a" || restored.Reason != "best-route")
        throw new InvalidOperationException("Routing decision did not survive repository restart.");

    Console.WriteLine("restart-safe routing decision durability PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}

sealed class TestFactory(DbContextOptions<PaymentRoutingDbContext> options) : IDbContextFactory<PaymentRoutingDbContext>
{
    public PaymentRoutingDbContext CreateDbContext() => new(options);
    public Task<PaymentRoutingDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new PaymentRoutingDbContext(options));
}
