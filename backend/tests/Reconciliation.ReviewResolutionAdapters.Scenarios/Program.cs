using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Review;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Records;
using Reconciliation.Infrastructure.DataSources;
using Reconciliation.Infrastructure.Repositories;
using Reconciliation.Infrastructure.ReviewResolution;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var dbPath = Path.Combine(Path.GetTempPath(), $"afw-review-resolution-{Guid.NewGuid():N}.db");
try
{
    var options = new DbContextOptionsBuilder<ReconciliationReviewDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    await using var db = new ReconciliationReviewDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var reviewRepository = new EfReconciliationReviewRepository(db);
    var queuedAtUtc = new DateTime(2026, 9, 17, 20, 0, 0, DateTimeKind.Utc);
    var reviewId = Guid.NewGuid();
    var review = new ReconciliationReviewItem(
        reviewId,
        "partner-1",
        "internal-1",
        "external-1",
        ReconciliationMatchType.Exact,
        100,
        0,
        TimeSpan.FromSeconds(20),
        queuedAtUtc,
        ReconciliationReviewStatus.Approved,
        "reviewer-1",
        null,
        queuedAtUtc.AddMinutes(1));

    await reviewRepository.AddQueueAsync(new ReconciliationReviewQueue(
        "partner-1",
        queuedAtUtc.AddHours(-1),
        queuedAtUtc,
        [review]));

    var reader = new DurableReviewResolutionReviewReader(reviewRepository);
    var durable = await reader.GetAsync(reviewId);
    Assert(durable == review, "Durable review reader must return the stored review unchanged.");

    var dataSource = new SandboxReconciliationDataSource();
    dataSource.AddInternal(new InternalFinancialRecord(
        "internal-1", "ref-1", "partner-1", "EUR", 10_000,
        queuedAtUtc.AddMinutes(-2), "ledger"));
    dataSource.AddExternal(new ExternalFinancialRecord(
        "external-1", "ext-1", "partner-1", "EUR", 10_000,
        queuedAtUtc.AddMinutes(-1), "partner-feed"));

    var matcher = new ReconciliationDataReviewResolutionEvidenceMatcher(dataSource);
    var evidence = await matcher.FindMatchingEvidenceAsync(review);
    Assert(evidence is not null, "Concrete matcher must resolve evidence from reconciliation data.");
    Assert(evidence!.InternalRecordId == "internal-1", "Internal evidence id mismatch.");
    Assert(evidence.ExternalRecordId == "external-1", "External evidence id mismatch.");
    Assert(evidence.ObservedAtUtc == queuedAtUtc.AddMinutes(-1), "Evidence observation time must use latest matched record.");

    var store = new EfReviewResolutionStore(db);
    var service = new ReviewResolutionApplicationService(reader, matcher, store);
    var resolvedAtUtc = queuedAtUtc.AddMinutes(5);
    var result = await service.ResolveAsync(new ResolveReviewCommand(reviewId, "resolver-1", resolvedAtUtc));
    Assert(result.Status == Reconciliation.Domain.ReviewResolution.ReviewResolutionStatus.Resolved, "Approved review with evidence must resolve.");
    Assert(result.Resolution is not null, "Resolution record is required.");

    var stored = await store.GetAsync(reviewId);
    Assert(stored == result.Resolution, "Resolution store must round-trip the durable resolution.");

    var repeated = await service.ResolveAsync(new ResolveReviewCommand(reviewId, "resolver-2", resolvedAtUtc.AddMinutes(1)));
    Assert(repeated.Status == Reconciliation.Domain.ReviewResolution.ReviewResolutionStatus.AlreadyResolved,
        "A durable resolution must make retries idempotent.");
    Assert(repeated.Resolution == stored, "Retry must preserve the original durable resolution.");

    var duplicateAdded = await store.TryAddAsync(stored! with { ResolvedBy = "resolver-other" });
    Assert(!duplicateAdded, "Resolution store must reject a second row for the same review.");

    var missingReviewId = Guid.NewGuid();
    var missingReview = review with
    {
        ReviewId = missingReviewId,
        InternalRecordId = "internal-missing",
        ExternalRecordId = null
    };
    await reviewRepository.AddQueueAsync(new ReconciliationReviewQueue(
        "partner-1", queuedAtUtc.AddHours(-1), queuedAtUtc, [missingReview]));
    var missing = await service.ResolveAsync(new ResolveReviewCommand(missingReviewId, "resolver-1", resolvedAtUtc));
    Assert(missing.Status == Reconciliation.Domain.ReviewResolution.ReviewResolutionStatus.NoMatchingEvidence,
        "Missing reconciliation evidence must not create a resolution.");
    Assert(await store.GetAsync(missingReviewId) is null, "No-evidence review must remain unresolved.");

    using var cts = new CancellationTokenSource();
    cts.Cancel();
    try
    {
        await reader.GetAsync(reviewId, cts.Token);
        throw new InvalidOperationException("Expected cancellation.");
    }
    catch (OperationCanceledException) { }

    Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-5 concrete review/evidence adapters and resolution persistence scenarios: PASS");
}
finally
{
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
