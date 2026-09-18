using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Resolutions;
using Reconciliation.Infrastructure.Resolutions;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var databasePath = Path.Combine(Path.GetTempPath(), $"afw-reconciliation-resolution-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={databasePath}";
var options = new DbContextOptionsBuilder<ReconciliationResolutionDbContext>().UseSqlite(connectionString).Options;

try
{
    await using (var setup = new ReconciliationResolutionDbContext(options))
    {
        await setup.Database.EnsureDeletedAsync();
        await setup.Database.EnsureCreatedAsync();
    }

    var at = new DateTime(2026, 9, 18, 9, 15, 0, DateTimeKind.Utc);
    var one = ReconciliationResolution.Create(
        Guid.NewGuid(), "partner-one", "internal-42", "external-42",
        ReconciliationResolutionDisposition.RecordCorrected,
        "partner statement corrected", "evidence://42", "resolver-1", at);
    var two = ReconciliationResolution.Create(
        Guid.NewGuid(), "partner-two", "internal-43", "external-43",
        ReconciliationResolutionDisposition.VarianceAccepted,
        "variance accepted", "evidence://43", "resolver-2", at.AddMinutes(5));

    await using (var write = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(write);
        await repository.AddAsync(one);
        await repository.AddAsync(two);
    }

    await using (var read = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(read);
        var loaded = await repository.GetByReviewIdAsync(one.ReviewId);
        Assert(loaded?.ResolutionId == one.ResolutionId, "Resolution must round-trip.");

        var partnerOne = await repository.ListAsync(new ReconciliationResolutionRepositoryQuery(
            ["partner-one"], null, null, null, null, 100));
        Assert(partnerOne.Count == 1 && partnerOne[0].PartnerId == "partner-one", "Partner filter must be durable.");

        var variance = await repository.ListAsync(new ReconciliationResolutionRepositoryQuery(
            null, ReconciliationResolutionDisposition.VarianceAccepted, null, null, null, 100));
        Assert(variance.Count == 1 && variance[0].ResolutionId == two.ResolutionId, "Disposition filter must work.");

        var resolver = await repository.ListAsync(new ReconciliationResolutionRepositoryQuery(
            null, null, "resolver-1", null, null, 100));
        Assert(resolver.Count == 1 && resolver[0].ResolutionId == one.ResolutionId, "Resolver filter must work.");

        var window = await repository.ListAsync(new ReconciliationResolutionRepositoryQuery(
            null, null, null, at.AddMinutes(1), at.AddMinutes(10), 100));
        Assert(window.Count == 1 && window[0].ResolutionId == two.ResolutionId, "Operational time window must work.");

        var audit = await repository.ListByReviewIdAsync(one.ReviewId);
        Assert(audit.Count == 1, "Resolution creation must append exactly one audit entry.");
    }

    await using (var duplicateContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(duplicateContext);
        try
        {
            await repository.AddAsync(ReconciliationResolution.Create(
                one.ReviewId, "partner-one", "internal-42", "external-42",
                ReconciliationResolutionDisposition.VarianceAccepted,
                "different resolution", "evidence://99", "resolver-2", at.AddMinutes(1)));
            throw new InvalidOperationException("Expected durable ReviewId uniqueness violation.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("durable reconciliation resolution", StringComparison.OrdinalIgnoreCase)) { }
    }

    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await using (var cancellationContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(cancellationContext);
        try
        {
            await repository.ListAsync(new ReconciliationResolutionRepositoryQuery(null, null, null, null, null, 10), cts.Token);
            throw new InvalidOperationException("Expected cancellation.");
        }
        catch (OperationCanceledException) { }
    }

    Console.WriteLine("AFW-BE-RECONCILIATION-RESOLUTION-1 durable persistence and operational query scenarios: PASS");
}
finally
{
    if (File.Exists(databasePath)) File.Delete(databasePath);
    if (File.Exists(databasePath + "-shm")) File.Delete(databasePath + "-shm");
    if (File.Exists(databasePath + "-wal")) File.Delete(databasePath + "-wal");
}
