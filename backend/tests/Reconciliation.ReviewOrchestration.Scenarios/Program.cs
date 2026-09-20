using Reconciliation.Application.Matching;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;
using Reconciliation.Infrastructure.Repositories;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var fromUtc = new DateTime(2026, 9, 17, 18, 0, 0, DateTimeKind.Utc);
var toUtc = fromUtc.AddHours(1);
var queuedAtUtc = toUtc.AddMinutes(1);
var repository = new InMemoryReconciliationReviewRepository();
var queueService = new ReconciliationReviewQueueService();
var service = new ReconciliationReviewApplicationService(repository, queueService);

var batch = new AutomaticCandidateClassificationBatch(
    "partner-1", fromUtc, toUtc,
    [
        new AutomaticCandidateClassification("i-1", "e-1", ReconciliationMatchType.Exact, 100, 0, TimeSpan.Zero),
        new AutomaticCandidateClassification("i-2", "e-2", ReconciliationMatchType.Partial, 65, -100, TimeSpan.FromMinutes(3)),
        new AutomaticCandidateClassification("i-3", null, ReconciliationMatchType.Unmatched, 0, null, null)
    ]);

var queue = await service.EnqueueAsync(batch, queuedAtUtc);
Assert(queue.Items.Count == 3, "Three review items must be stored.");
var pending = await service.ListAsync("partner-1", ReconciliationReviewStatus.PendingReview);
Assert(pending.Count == 3, "All new items must be pending.");
Assert((await service.ListAsync("unknown")).Count == 0, "Unknown partner must have no items.");

var first = pending[0];
Assert((await service.GetAsync(first.ReviewId))?.ReviewId == first.ReviewId, "Review must be readable by id.");
var approved = await service.DecideAsync(new ReconciliationReviewDecisionCommand(
    first.ReviewId, ReconciliationReviewStatus.Approved, "reviewer-1", null, queuedAtUtc.AddMinutes(1)));
Assert(approved?.Status == ReconciliationReviewStatus.Approved, "Review must be approved.");
Assert((await service.ListAsync("partner-1", ReconciliationReviewStatus.Approved)).Count == 1,
    "Approved filter must return decision.");

var second = pending[1];
var rejected = await service.DecideAsync(new ReconciliationReviewDecisionCommand(
    second.ReviewId, ReconciliationReviewStatus.Rejected, "reviewer-2", "amount mismatch", queuedAtUtc.AddMinutes(2)));
Assert(rejected?.DecisionReason == "amount mismatch", "Reject reason must persist.");

var third = pending[2];
var escalated = await service.DecideAsync(new ReconciliationReviewDecisionCommand(
    third.ReviewId, ReconciliationReviewStatus.Escalated, "reviewer-3", "partner investigation", queuedAtUtc.AddMinutes(3)));
Assert(escalated?.Status == ReconciliationReviewStatus.Escalated, "Review must be escalated.");
var resolved = await service.DecideAsync(new ReconciliationReviewDecisionCommand(
    third.ReviewId, ReconciliationReviewStatus.Approved, "senior-reviewer", "verified", queuedAtUtc.AddMinutes(4)));
Assert(resolved?.Status == ReconciliationReviewStatus.Approved, "Escalated review must be resolvable.");

Assert(await service.DecideAsync(new ReconciliationReviewDecisionCommand(
    Guid.NewGuid(), ReconciliationReviewStatus.Approved, "reviewer", null, queuedAtUtc)) is null,
    "Unknown review decision must return null.");

var stale = queue.Items[1];
var staleReplacement = stale with
{
    Status = ReconciliationReviewStatus.Approved,
    ReviewerId = "stale-reviewer",
    DecidedAtUtc = queuedAtUtc.AddMinutes(5)
};
Assert(!await repository.TryReplaceAsync(staleReplacement, ReconciliationReviewStatus.PendingReview),
    "Conditional replace must reject stale expected status.");

using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await service.ListAsync("partner-1", null, cts.Token);
    throw new InvalidOperationException("Expected cancellation.");
}
catch (OperationCanceledException) { }

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-3 review orchestration scenarios: PASS");
