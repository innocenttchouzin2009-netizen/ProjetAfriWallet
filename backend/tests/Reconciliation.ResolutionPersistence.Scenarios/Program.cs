using Microsoft.EntityFrameworkCore;
using Reconciliation.Domain.Resolutions;
using Reconciliation.Infrastructure.Resolutions;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var databasePath = Path.Combine(Path.GetTempPath(), $"afw-reconciliation-resolution-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={databasePath}";
var options = new DbContextOptionsBuilder<ReconciliationResolutionDbContext>()
    .UseSqlite(connectionString)
    .Options;

try
{
    await using (var setup = new ReconciliationResolutionDbContext(options))
    {
        await setup.Database.EnsureDeletedAsync();
        await setup.Database.EnsureCreatedAsync();
    }

    var reviewId = Guid.NewGuid();
    var resolvedAt = new DateTime(2026, 9, 18, 9, 15, 0, DateTimeKind.Utc);
    var resolution = ReconciliationResolution.Create(
        reviewId,
        "partner-one",
        "internal-42",
        "external-42",
        ReconciliationResolutionDisposition.RecordCorrected,
        "partner statement corrected and replayed",
        "evidence://statement/2026-09-18/42",
        "resolver-1",
        resolvedAt);

    await using (var writeContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(writeContext);
        await repository.AddAsync(resolution);
    }

    await using (var readContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(readContext);
        var loaded = await repository.GetByReviewIdAsync(reviewId);

        Assert(loaded is not null, "Persisted resolution must be readable.");
        Assert(loaded!.ResolutionId == resolution.ResolutionId, "Resolution id must round-trip.");
        Assert(loaded.ReviewId == reviewId, "Review id must round-trip.");
        Assert(loaded.PartnerId == "partner-one", "Partner id must round-trip.");
        Assert(loaded.InternalRecordId == "internal-42", "Internal record id must round-trip.");
        Assert(loaded.ExternalRecordId == "external-42", "External record id must round-trip.");
        Assert(loaded.Disposition == ReconciliationResolutionDisposition.RecordCorrected, "Disposition must round-trip.");
        Assert(loaded.ResolvedAtUtc == resolvedAt && loaded.ResolvedAtUtc.Kind == DateTimeKind.Utc, "Resolution timestamp must rehydrate as UTC.");

        var audit = await repository.ListByReviewIdAsync(reviewId);
        Assert(audit.Count == 1, "Resolution creation must append exactly one audit entry.");
        Assert(audit[0].ResolutionId == resolution.ResolutionId, "Audit must reference resolution id.");
        Assert(audit[0].ReviewId == reviewId, "Audit must reference review id.");
        Assert(audit[0].Disposition == ReconciliationResolutionDisposition.RecordCorrected, "Audit disposition mismatch.");
        Assert(audit[0].ResolvedBy == "resolver-1", "Audit resolver mismatch.");
        Assert(audit[0].EvidenceReference == "evidence://statement/2026-09-18/42", "Audit evidence mismatch.");
        Assert(audit[0].RecordedAtUtc == resolvedAt && audit[0].RecordedAtUtc.Kind == DateTimeKind.Utc, "Audit timestamp must rehydrate as UTC.");
    }

    await using (var duplicateContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(duplicateContext);
        var duplicate = ReconciliationResolution.Create(
            reviewId,
            "partner-one",
            "internal-42",
            "external-42",
            ReconciliationResolutionDisposition.VarianceAccepted,
            "different durable resolution",
            "evidence://statement/2026-09-18/99",
            "resolver-2",
            resolvedAt.AddMinutes(1));

        try
        {
            await repository.AddAsync(duplicate);
            throw new InvalidOperationException("Expected durable ReviewId uniqueness violation.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("durable reconciliation resolution", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    await using (var verifyContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(verifyContext);
        var audit = await repository.ListByReviewIdAsync(reviewId);
        Assert(audit.Count == 1, "Failed duplicate write must not append an audit entry.");

        var storedResolutionCount = await verifyContext.Resolutions.AsNoTracking().CountAsync(x => x.ReviewId == reviewId);
        var storedAuditCount = await verifyContext.Audit.AsNoTracking().CountAsync(x => x.ReviewId == reviewId);
        Assert(storedResolutionCount == 1, "Exactly one durable resolution must exist per review.");
        Assert(storedAuditCount == 1, "Audit history must remain append-only and transactionally consistent.");
    }

    await using (var unknownContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(unknownContext);
        Assert(await repository.GetByReviewIdAsync(Guid.NewGuid()) is null, "Unknown review must return no resolution.");
        Assert((await repository.ListByReviewIdAsync(Guid.NewGuid())).Count == 0, "Unknown review must return empty audit history.");
    }

    await using (var cancellationContext = new ReconciliationResolutionDbContext(options))
    {
        var repository = new EfReconciliationResolutionRepository(cancellationContext);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await repository.GetByReviewIdAsync(reviewId, cts.Token);
            throw new InvalidOperationException("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    Console.WriteLine("AFW-BE-RECONCILIATION-RESOLUTION-1 durable resolution persistence and audit scenarios: PASS");
}
finally
{
    if (File.Exists(databasePath)) File.Delete(databasePath);
    if (File.Exists(databasePath + "-shm")) File.Delete(databasePath + "-shm");
    if (File.Exists(databasePath + "-wal")) File.Delete(databasePath + "-wal");
}
