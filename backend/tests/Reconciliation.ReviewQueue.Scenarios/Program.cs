using Reconciliation.Application.Matching;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

var fromUtc = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
var toUtc = fromUtc.AddHours(1);
var queuedAtUtc = toUtc.AddMinutes(1);

var classified = new AutomaticCandidateClassificationBatch(
    "partner-1",
    fromUtc,
    toUtc,
    [
        new AutomaticCandidateClassification("i-exact", "e-exact", ReconciliationMatchType.Exact, 100, 0, TimeSpan.FromMinutes(2)),
        new AutomaticCandidateClassification("i-partial", "e-partial", ReconciliationMatchType.Partial, 60, -150, TimeSpan.FromMinutes(2)),
        new AutomaticCandidateClassification("i-unmatched", null, ReconciliationMatchType.Unmatched, 0, null, null)
    ]);

var service = new ReconciliationReviewQueueService();
var queue = service.BuildQueue(classified, queuedAtUtc);

Assert(queue.PartnerId == "partner-1", "Partner must be preserved in review queue.");
Assert(queue.FromUtc == fromUtc && queue.ToUtc == toUtc, "Review queue window must be preserved.");
Assert(queue.Items.Count == 3, "Every classification must become one review item.");
Assert(queue.Items.All(item => item.Status == ReconciliationReviewStatus.PendingReview), "New review items must start PendingReview.");
Assert(queue.Items.All(item => item.ReviewId != Guid.Empty), "Every review item must have an id.");

var exact = queue.Items.Single(item => item.InternalRecordId == "i-exact");
Assert(exact.MatchType == ReconciliationMatchType.Exact && exact.ConfidenceScore == 100,
    "Exact classification metadata must be preserved.");
Assert(exact.ExternalRecordId == "e-exact", "External record id must be preserved.");

var partial = queue.Items.Single(item => item.InternalRecordId == "i-partial");
Assert(partial.AmountDifferenceMinor == -150, "Amount difference must remain available for review.");
Assert(partial.TimeDifference == TimeSpan.FromMinutes(2), "Time difference must remain available for review.");

var approved = service.ApplyDecision(exact, new ReconciliationReviewDecisionCommand(
    exact.ReviewId,
    ReconciliationReviewStatus.Approved,
    "reviewer-1",
    null,
    queuedAtUtc.AddMinutes(1)));
Assert(approved.Status == ReconciliationReviewStatus.Approved, "Pending item must be approvable.");
Assert(approved.ReviewerId == "reviewer-1", "Reviewer must be recorded.");
Assert(approved.DecidedAtUtc == queuedAtUtc.AddMinutes(1), "Decision time must be recorded.");
Assert(exact.Status == ReconciliationReviewStatus.PendingReview, "Decision must not mutate the source review record.");
AssertThrows<InvalidOperationException>(
    () => service.ApplyDecision(approved, new ReconciliationReviewDecisionCommand(
        approved.ReviewId,
        ReconciliationReviewStatus.Rejected,
        "reviewer-2",
        "changed mind",
        queuedAtUtc.AddMinutes(2))),
    "Approved review item must be terminal.");

var rejected = service.ApplyDecision(partial, new ReconciliationReviewDecisionCommand(
    partial.ReviewId,
    ReconciliationReviewStatus.Rejected,
    "reviewer-2",
    "amount mismatch requires rejection",
    queuedAtUtc.AddMinutes(2)));
Assert(rejected.Status == ReconciliationReviewStatus.Rejected, "Pending item must be rejectable.");
Assert(rejected.DecisionReason == "amount mismatch requires rejection", "Reject reason must be preserved.");

var unmatched = queue.Items.Single(item => item.InternalRecordId == "i-unmatched");
var escalated = service.ApplyDecision(unmatched, new ReconciliationReviewDecisionCommand(
    unmatched.ReviewId,
    ReconciliationReviewStatus.Escalated,
    "reviewer-3",
    "manual partner investigation required",
    queuedAtUtc.AddMinutes(3)));
Assert(escalated.Status == ReconciliationReviewStatus.Escalated, "Pending item must be escalatable.");

var escalatedApproved = service.ApplyDecision(escalated, new ReconciliationReviewDecisionCommand(
    escalated.ReviewId,
    ReconciliationReviewStatus.Approved,
    "senior-reviewer",
    "verified externally",
    queuedAtUtc.AddMinutes(4)));
Assert(escalatedApproved.Status == ReconciliationReviewStatus.Approved,
    "Escalated item must be resolvable by approval.");

AssertThrows<ArgumentException>(
    () => service.ApplyDecision(partial, new ReconciliationReviewDecisionCommand(
        partial.ReviewId,
        ReconciliationReviewStatus.Rejected,
        "reviewer-2",
        null,
        queuedAtUtc.AddMinutes(2))),
    "Reject must require a reason.");
AssertThrows<ArgumentException>(
    () => service.ApplyDecision(unmatched, new ReconciliationReviewDecisionCommand(
        unmatched.ReviewId,
        ReconciliationReviewStatus.Escalated,
        "reviewer-3",
        " ",
        queuedAtUtc.AddMinutes(3))),
    "Escalate must require a reason.");
AssertThrows<ArgumentException>(
    () => service.ApplyDecision(exact, new ReconciliationReviewDecisionCommand(
        exact.ReviewId,
        ReconciliationReviewStatus.PendingReview,
        "reviewer-1",
        null,
        queuedAtUtc.AddMinutes(1))),
    "PendingReview must not be accepted as a decision.");
AssertThrows<ArgumentException>(
    () => service.ApplyDecision(exact, new ReconciliationReviewDecisionCommand(
        exact.ReviewId,
        ReconciliationReviewStatus.Approved,
        "reviewer-1",
        null,
        DateTime.SpecifyKind(queuedAtUtc.AddMinutes(1), DateTimeKind.Local))),
    "Decision timestamp must be UTC.");
AssertThrows<ArgumentException>(
    () => service.ApplyDecision(exact, new ReconciliationReviewDecisionCommand(
        exact.ReviewId,
        ReconciliationReviewStatus.Approved,
        "reviewer-1",
        null,
        queuedAtUtc.AddMinutes(-1))),
    "Decision time cannot precede queue time.");
AssertThrows<ArgumentException>(
    () => service.ApplyDecision(exact, new ReconciliationReviewDecisionCommand(
        Guid.NewGuid(),
        ReconciliationReviewStatus.Approved,
        "reviewer-1",
        null,
        queuedAtUtc.AddMinutes(1))),
    "Decision review id must match the item.");

var pending = service.FilterByStatus(queue, ReconciliationReviewStatus.PendingReview);
Assert(pending.Count == 3, "Filtering the immutable source queue must return all original pending items.");

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-3 review queue and decision model scenarios: PASS");
