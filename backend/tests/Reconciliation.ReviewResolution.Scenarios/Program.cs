using Reconciliation.Application.Review;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.ReviewResolution;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var reviewId = Guid.NewGuid();
var queuedAt = new DateTime(2026, 9, 17, 20, 0, 0, DateTimeKind.Utc);
var approved = Review(reviewId, ReconciliationReviewStatus.Approved, queuedAt);
var evidence = new ReviewResolutionEvidence("evidence-1", "internal-1", null, queuedAt.AddMinutes(1));
var store = new InMemoryResolutionStore();
var service = new ReviewResolutionApplicationService(
    new FixedReviewReader(approved),
    new FixedEvidenceMatcher(evidence),
    store);

var resolved = await service.ResolveAsync(new ResolveReviewCommand(reviewId, "reviewer-1", queuedAt.AddMinutes(2)));
Assert(resolved.Status == ReviewResolutionStatus.Resolved, "Approved review with evidence must resolve.");
Assert(resolved.Resolution?.EvidenceId == "evidence-1", "Resolution must preserve evidence id.");
Assert(store.AddCalls == 1, "Resolution must be stored once.");

var replay = await service.ResolveAsync(new ResolveReviewCommand(reviewId, "reviewer-1", queuedAt.AddMinutes(3)));
Assert(replay.Status == ReviewResolutionStatus.AlreadyResolved, "Replay must be idempotent.");
Assert(store.AddCalls == 1, "Replay must not store twice.");

var missingId = Guid.NewGuid();
var missing = await new ReviewResolutionApplicationService(
    new FixedReviewReader(null), new FixedEvidenceMatcher(evidence), new InMemoryResolutionStore())
    .ResolveAsync(new ResolveReviewCommand(missingId, "reviewer", queuedAt.AddMinutes(2)));
Assert(missing.Status == ReviewResolutionStatus.NotEligible, "Missing review must be not eligible.");

var approvedId = Guid.NewGuid();
var noMatch = await new ReviewResolutionApplicationService(
    new FixedReviewReader(Review(approvedId, ReconciliationReviewStatus.Approved, queuedAt)),
    new FixedEvidenceMatcher(null), new InMemoryResolutionStore())
    .ResolveAsync(new ResolveReviewCommand(approvedId, "reviewer", queuedAt.AddMinutes(2)));
Assert(noMatch.Status == ReviewResolutionStatus.NoMatchingEvidence, "Approved review without evidence must return no match.");

foreach (var status in new[] { ReconciliationReviewStatus.PendingReview, ReconciliationReviewStatus.Rejected, ReconciliationReviewStatus.Escalated })
{
    var id = Guid.NewGuid();
    var result = await new ReviewResolutionApplicationService(
        new FixedReviewReader(Review(id, status, queuedAt)),
        new FixedEvidenceMatcher(evidence), new InMemoryResolutionStore())
        .ResolveAsync(new ResolveReviewCommand(id, "reviewer", queuedAt.AddMinutes(2)));
    Assert(result.Status == ReviewResolutionStatus.NotEligible, $"{status} must not be eligible.");
}

var mismatchId = Guid.NewGuid();
var mismatch = await new ReviewResolutionApplicationService(
    new FixedReviewReader(Review(mismatchId, ReconciliationReviewStatus.Approved, queuedAt)),
    new FixedEvidenceMatcher(new ReviewResolutionEvidence("evidence-2", "different", "different", queuedAt.AddMinutes(1))),
    new InMemoryResolutionStore())
    .ResolveAsync(new ResolveReviewCommand(mismatchId, "reviewer", queuedAt.AddMinutes(2)));
Assert(mismatch.Status == ReviewResolutionStatus.NoMatchingEvidence, "Mismatched evidence must not resolve review.");

var raceId = Guid.NewGuid();
var raceExisting = new ReviewResolutionRecord(raceId, "partner-1", "evidence-race", "other", queuedAt.AddMinutes(2));
var raceStore = new InMemoryResolutionStore { FailFirstAddWith = raceExisting };
var race = await new ReviewResolutionApplicationService(
    new FixedReviewReader(Review(raceId, ReconciliationReviewStatus.Approved, queuedAt)),
    new FixedEvidenceMatcher(new ReviewResolutionEvidence("evidence-race", "internal-1", null, queuedAt.AddMinutes(1))),
    raceStore)
    .ResolveAsync(new ResolveReviewCommand(raceId, "reviewer", queuedAt.AddMinutes(2)));
Assert(race.Status == ReviewResolutionStatus.AlreadyResolved, "Concurrent insert must converge to already resolved.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.ResolveAsync(new ResolveReviewCommand(Guid.NewGuid(), "reviewer", queuedAt), cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-5 review resolution foundation scenarios: PASS");

static ReconciliationReviewItem Review(Guid id, ReconciliationReviewStatus status, DateTime queuedAt) => new(
    id, "partner-1", "internal-1", "external-1", ReconciliationMatchType.Partial, 80, 0, TimeSpan.Zero,
    queuedAt, status, status == ReconciliationReviewStatus.PendingReview ? null : "reviewer", null,
    status == ReconciliationReviewStatus.PendingReview ? null : queuedAt.AddMinutes(1));

sealed class FixedReviewReader(ReconciliationReviewItem? item) : IReviewResolutionReviewReader
{
    public Task<ReconciliationReviewItem?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(item?.ReviewId == reviewId ? item : null);
    }
}

sealed class FixedEvidenceMatcher(ReviewResolutionEvidence? evidence) : IReviewResolutionEvidenceMatcher
{
    public Task<ReviewResolutionEvidence?> FindMatchingEvidenceAsync(ReconciliationReviewItem review, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(evidence);
    }
}

sealed class InMemoryResolutionStore : IReviewResolutionStore
{
    private readonly Dictionary<Guid, ReviewResolutionRecord> values = new();
    public int AddCalls { get; private set; }
    public ReviewResolutionRecord? FailFirstAddWith { get; init; }

    public Task<ReviewResolutionRecord?> GetAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values.TryGetValue(reviewId, out var value);
        return Task.FromResult(value);
    }

    public Task<bool> TryAddAsync(ReviewResolutionRecord resolution, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddCalls++;
        if (FailFirstAddWith is not null && AddCalls == 1)
        {
            values[resolution.ReviewId] = FailFirstAddWith;
            return Task.FromResult(false);
        }
        if (values.ContainsKey(resolution.ReviewId)) return Task.FromResult(false);
        values[resolution.ReviewId] = resolution;
        return Task.FromResult(true);
    }
}
