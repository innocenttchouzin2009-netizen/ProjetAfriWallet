using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;
using Reconciliation.Infrastructure.Repositories;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var databasePath = Path.Combine(Path.GetTempPath(), $"afw-reconciliation-review-{Guid.NewGuid():N}.db");
var connectionString = $"Data Source={databasePath}";
var options = new DbContextOptionsBuilder<ReconciliationReviewDbContext>()
    .UseSqlite(connectionString)
    .Options;

try
{
    await using (var setup = new ReconciliationReviewDbContext(options))
    {
        await setup.Database.EnsureDeletedAsync();
        await setup.Database.EnsureCreatedAsync();
    }

    var queuedAt = new DateTime(2026, 9, 17, 19, 30, 0, DateTimeKind.Utc);
    var firstId = Guid.NewGuid();
    var secondId = Guid.NewGuid();
    var queue = new ReconciliationReviewQueue(
        "partner-one",
        queuedAt.AddHours(-1),
        queuedAt,
        [
            new ReconciliationReviewItem(firstId, "partner-one", "internal-1", "external-1", ReconciliationMatchType.Partial, 82, 25, TimeSpan.FromSeconds(15), queuedAt, ReconciliationReviewStatus.PendingReview, null, null, null),
            new ReconciliationReviewItem(secondId, "partner-one", null, "external-2", ReconciliationMatchType.Unmatched, 20, null, null, queuedAt.AddSeconds(1), ReconciliationReviewStatus.PendingReview, null, null, null)
        ]);

    await using (var writeContext = new ReconciliationReviewDbContext(options))
    {
        var repository = new EfReconciliationReviewRepository(writeContext);
        await repository.AddQueueAsync(queue);
    }

    await using (var readContext = new ReconciliationReviewDbContext(options))
    {
        var repository = new EfReconciliationReviewRepository(readContext);
        var loaded = await repository.GetAsync(firstId);
        Assert(loaded is not null, "Persisted review item must be readable.");
        Assert(loaded!.PartnerId == "partner-one", "Partner must round-trip.");
        Assert(loaded.MatchType == ReconciliationMatchType.Partial, "Match type must round-trip.");
        Assert(loaded.TimeDifference == TimeSpan.FromSeconds(15), "Time difference must round-trip.");
        Assert(loaded.QueuedAtUtc.Kind == DateTimeKind.Utc, "Queued timestamp must rehydrate as UTC.");

        var listed = await repository.ListAsync("partner-one", ReconciliationReviewStatus.PendingReview);
        Assert(listed.Count == 2, "Pending filter must return both queued items.");
        Assert(listed[0].ReviewId == firstId && listed[1].ReviewId == secondId, "List ordering must remain deterministic.");

        var decidedAt = queuedAt.AddMinutes(5);
        var approved = loaded with
        {
            Status = ReconciliationReviewStatus.Approved,
            ReviewerId = "reviewer-42",
            DecisionReason = "verified against partner statement",
            DecidedAtUtc = decidedAt
        };

        var replaced = await repository.TryReplaceAsync(approved, ReconciliationReviewStatus.PendingReview);
        Assert(replaced, "Expected-status replacement must succeed once.");

        var staleReplacement = approved with
        {
            Status = ReconciliationReviewStatus.Rejected,
            DecisionReason = "stale writer",
            DecidedAtUtc = decidedAt.AddSeconds(1)
        };
        var staleResult = await repository.TryReplaceAsync(staleReplacement, ReconciliationReviewStatus.PendingReview);
        Assert(!staleResult, "Stale expected status must fail closed.");
    }

    await using (var verifyContext = new ReconciliationReviewDbContext(options))
    {
        var repository = new EfReconciliationReviewRepository(verifyContext);
        var persisted = await repository.GetAsync(firstId);
        Assert(persisted?.Status == ReconciliationReviewStatus.Approved, "Approved status must survive a new DbContext.");
        Assert(persisted?.ReviewerId == "reviewer-42", "Reviewer must survive persistence.");
        Assert(persisted?.DecisionReason == "verified against partner statement", "Decision reason must survive persistence.");

        var audits = await verifyContext.ReviewAudit.AsNoTracking().Where(x => x.ReviewId == firstId).ToArrayAsync();
        Assert(audits.Length == 1, "Only the successful decision may append an audit row.");
        Assert(audits[0].PreviousStatus == (int)ReconciliationReviewStatus.PendingReview, "Audit previous status mismatch.");
        Assert(audits[0].NewStatus == (int)ReconciliationReviewStatus.Approved, "Audit new status mismatch.");
        Assert(audits[0].ReviewerId == "reviewer-42", "Audit reviewer mismatch.");

        var approvedOnly = await repository.ListAsync("partner-one", ReconciliationReviewStatus.Approved);
        Assert(approvedOnly.Count == 1 && approvedOnly[0].ReviewId == firstId, "Status filtering must reflect durable decisions.");
    }

    await using (var duplicateContext = new ReconciliationReviewDbContext(options))
    {
        var repository = new EfReconciliationReviewRepository(duplicateContext);
        try
        {
            await repository.AddQueueAsync(queue);
            throw new InvalidOperationException("Expected duplicate review insertion to fail.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exist", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    await using (var cancellationContext = new ReconciliationReviewDbContext(options))
    {
        var repository = new EfReconciliationReviewRepository(cancellationContext);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try
        {
            await repository.GetAsync(firstId, cts.Token);
            throw new InvalidOperationException("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-4 durable review persistence scenarios: PASS");
}
finally
{
    if (File.Exists(databasePath)) File.Delete(databasePath);
    if (File.Exists(databasePath + "-shm")) File.Delete(databasePath + "-shm");
    if (File.Exists(databasePath + "-wal")) File.Delete(databasePath + "-wal");
}
