using Reconciliation.Application.AutomaticResolution;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var queuedAt = new DateTime(2026, 9, 17, 21, 0, 0, DateTimeKind.Utc);
var decidedAt = queuedAt.AddMinutes(5);
var policy = new AutomaticResolutionPolicy();
var orchestrator = new AutomaticResolutionDecisionOrchestrator(policy);

var exact = new ReconciliationReviewItem(
    Guid.NewGuid(), "partner-a", "int-1", "ext-1", ReconciliationMatchType.Exact, 100, 0,
    TimeSpan.FromMinutes(2), queuedAt, ReconciliationReviewStatus.PendingReview, null, null, null);
var partial = exact with
{
    ReviewId = Guid.NewGuid(),
    MatchType = ReconciliationMatchType.Partial,
    ConfidenceScore = 80,
    AmountDifferenceMinor = 0
};
var unmatched = exact with
{
    ReviewId = Guid.NewGuid(),
    MatchType = ReconciliationMatchType.Unmatched,
    ConfidenceScore = 0,
    ExternalRecordId = null,
    AmountDifferenceMinor = null
};

var batch = orchestrator.Plan([exact, partial, unmatched], decidedAt);
Assert(batch.Decisions.Count == 3, "All review items must receive a policy disposition.");

var exactDecision = batch.Decisions.Single(x => x.ReviewId == exact.ReviewId);
Assert(exactDecision.Disposition == AutomaticResolutionDisposition.AutoApprove, "Exact match must auto-approve.");
Assert(exactDecision.Command?.Decision == ReconciliationReviewStatus.Approved, "Exact match must produce Approved command.");
Assert(exactDecision.Command?.ReviewerId == "system:auto-resolution", "Automatic decisions must use system actor.");

var partialDecision = batch.Decisions.Single(x => x.ReviewId == partial.ReviewId);
Assert(partialDecision.Disposition == AutomaticResolutionDisposition.ManualReview, "Partial match must remain manual.");
Assert(partialDecision.Command is null, "Manual review must not produce a decision command.");

var unmatchedDecision = batch.Decisions.Single(x => x.ReviewId == unmatched.ReviewId);
Assert(unmatchedDecision.Disposition == AutomaticResolutionDisposition.Escalate, "Unmatched item must escalate.");
Assert(unmatchedDecision.Command?.Decision == ReconciliationReviewStatus.Escalated, "Unmatched item must produce Escalated command.");
Assert(!string.IsNullOrWhiteSpace(unmatchedDecision.Command?.Reason), "Escalation must contain a reason.");

var alreadyApproved = exact with { ReviewId = Guid.NewGuid(), Status = ReconciliationReviewStatus.Approved };
var terminalDecision = orchestrator.Plan(alreadyApproved, decidedAt);
Assert(terminalDecision.Disposition == AutomaticResolutionDisposition.ManualReview, "Non-pending items must never be auto-decided again.");
Assert(terminalDecision.Command is null, "Non-pending items must not emit a new command.");

var suspiciousExact = exact with { ReviewId = Guid.NewGuid(), AmountDifferenceMinor = 1 };
Assert(policy.Evaluate(suspiciousExact).Disposition == AutomaticResolutionDisposition.ManualReview,
    "Exact classification with non-zero amount difference must fail closed to manual review.");

try
{
    orchestrator.Plan(exact, DateTime.SpecifyKind(decidedAt, DateTimeKind.Local));
    throw new InvalidOperationException("Expected non-UTC timestamp rejection.");
}
catch (ArgumentException) { }

try
{
    orchestrator.Plan(exact, queuedAt.AddSeconds(-1));
    throw new InvalidOperationException("Expected backwards timestamp rejection.");
}
catch (ArgumentException) { }

Console.WriteLine("Automatic resolution policy and decision orchestration scenarios: PASS");
