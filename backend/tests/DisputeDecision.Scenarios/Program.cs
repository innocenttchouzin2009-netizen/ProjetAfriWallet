using AfriWallet.Disputes.Decision.Application.Abstractions;
using AfriWallet.Disputes.Decision.Application.Commands;
using AfriWallet.Disputes.Decision.Application.Services;
using AfriWallet.Disputes.Decision.Domain.Decisions;
using AfriWallet.Disputes.Decision.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";

var investigations = new SandboxInvestigationOutcomeReader();
var repository = new InMemoryDisputeDecisionRepository();
var audit = new InMemoryDisputeDecisionAuditStore();
var service = new DisputeDecisionService(repository, investigations, audit, new FixedClock(now), new DisputeDecisionPolicy());

Guid Seed(string outcome, string classification, decimal disputedAmount, string status = "Completed", string currency = "USD")
{
    var investigationId = Guid.NewGuid();
    investigations.Set(new InvestigationOutcomeSnapshot(
        investigationId, Guid.NewGuid(), "AWID-DSP", status, outcome, classification, disputedAmount, currency, now.AddDays(-1)));
    return investigationId;
}

// 1-2: investigation gating.
var missingInvestigationBlocked = false;
try
{
    await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(Guid.NewGuid(), actor));
}
catch (KeyNotFoundException)
{
    missingInvestigationBlocked = true;
}
Check("evaluate blocked for missing investigation", missingInvestigationBlocked, ref passed);

var notCompletedBlocked = false;
try
{
    await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
        Seed("EvidenceSupportsClaim", "UnauthorizedTransaction", 100, status: "UnderReview"), actor));
}
catch (InvalidOperationException)
{
    notCompletedBlocked = true;
}
Check("evaluate blocked for incomplete investigation", notCompletedBlocked, ref passed);

// 3: evidence does not support claim -> decline.
var decline = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceDoesNotSupportClaim", "Other", 100), actor));
Check("decline decision type", decline.DecisionType == ResolutionDecisionType.Decline, ref passed);
Check("decline reason code", decline.ReasonCode == ResolutionReasonCode.EvidenceDoesNotSupportClaim, ref passed);
Check("decline status resolved", decline.Status == ResolutionDecisionStatus.Declined, ref passed);
Check("decline needs no approval", !decline.RequiresManualApproval, ref passed);

// 4: insufficient evidence -> manual review, pending approval.
var insufficient = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("InsufficientEvidence", "Other", 100), actor));
Check("insufficient evidence manual review", insufficient.DecisionType == ResolutionDecisionType.ManualReview, ref passed);
Check("insufficient evidence pending approval", insufficient.Status == ResolutionDecisionStatus.PendingManualApproval, ref passed);

// 5: manual escalation required.
var escalation = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("ManualEscalationRequired", "Other", 100), actor));
Check("escalation reason code", escalation.ReasonCode == ResolutionReasonCode.InvestigationRequiresEscalation, ref passed);

// 6: unrecognized outcome -> manual review.
var unknownOutcome = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("SomethingUnexpected", "Other", 100), actor));
Check("unknown outcome policy manual review", unknownOutcome.ReasonCode == ResolutionReasonCode.PolicyRequiresManualReview, ref passed);

// 7-9: classification-driven recommendations below the manual approval threshold.
var unauthorizedLow = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceSupportsClaim", "UnauthorizedTransaction", 100), actor));
Check("unauthorized transaction chargeback recommended", unauthorizedLow.DecisionType == ResolutionDecisionType.ChargebackRecommended, ref passed);
Check("unauthorized transaction auto-approved below threshold", unauthorizedLow.Status == ResolutionDecisionStatus.Approved, ref passed);

var duplicateLow = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceSupportsClaim", "DuplicateTransaction", 100), actor));
Check("duplicate transaction refund recommended", duplicateLow.DecisionType == ResolutionDecisionType.RefundRecommended, ref passed);

var processingErrorLow = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceSupportsClaim", "ProcessingError", 100), actor));
Check("processing error refund recommended", processingErrorLow.ReasonCode == ResolutionReasonCode.ProcessingError, ref passed);

var refundNotProcessedLow = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceSupportsClaim", "RefundNotProcessed", 100), actor));
Check("refund not processed chargeback recommended", refundNotProcessedLow.DecisionType == ResolutionDecisionType.ChargebackRecommended, ref passed);

// 10: high-value threshold requires manual approval.
var unauthorizedHigh = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceSupportsClaim", "UnauthorizedTransaction", 1_000), actor));
Check("high value requires manual approval", unauthorizedHigh.RequiresManualApproval, ref passed);
Check("high value pending approval status", unauthorizedHigh.Status == ResolutionDecisionStatus.PendingManualApproval, ref passed);

// 11: unsupported classification always requires manual review regardless of amount.
var unsupported = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(
    Seed("EvidenceSupportsClaim", "SomeUnknownCategory", 10), actor));
Check("unsupported classification manual review", unsupported.ReasonCode == ResolutionReasonCode.UnsupportedClassification, ref passed);
Check("unsupported classification requires approval", unsupported.RequiresManualApproval, ref passed);

// 12-13: policy versioning and explainability.
Check("policy version stamped", unauthorizedLow.PolicyVersion == "AFW-DISPUTE-RESOLUTION:1.0", ref passed);
Check("decision explainable with factors", unauthorizedLow.FactorCount >= 3, ref passed);

// 14: idempotent evaluation returns the same active decision.
var idempotentInvestigationId = Seed("EvidenceSupportsClaim", "DuplicateTransaction", 50);
var first = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(idempotentInvestigationId, actor));
var second = await service.EvaluateAsync(new EvaluateDisputeDecisionCommand(idempotentInvestigationId, actor));
Check("idempotent evaluation returns same decision", first.DecisionId == second.DecisionId, ref passed);

// 15-16: manual approval workflow.
var approved = await service.ApproveAsync(new ApproveDisputeDecisionCommand(insufficient.DecisionId, "supervisor-1", "Reviewed evidence", actor));
Check("manual approval resolves status", approved.Status == ResolutionDecisionStatus.Approved, ref passed);

var approveNotRequiredBlocked = false;
try
{
    await service.ApproveAsync(new ApproveDisputeDecisionCommand(unauthorizedLow.DecisionId, "supervisor-1", "note", actor));
}
catch (InvalidOperationException)
{
    approveNotRequiredBlocked = true;
}
Check("approval blocked when not required", approveNotRequiredBlocked, ref passed);

var approveTwiceBlocked = false;
try
{
    await service.ApproveAsync(new ApproveDisputeDecisionCommand(insufficient.DecisionId, "supervisor-2", "second", actor));
}
catch (InvalidOperationException)
{
    approveTwiceBlocked = true;
}
Check("re-approval blocked once resolved", approveTwiceBlocked, ref passed);

var approveMissingBlocked = false;
try
{
    await service.ApproveAsync(new ApproveDisputeDecisionCommand(Guid.NewGuid(), "supervisor-1", "note", actor));
}
catch (KeyNotFoundException)
{
    approveMissingBlocked = true;
}
Check("approval blocked for missing decision", approveMissingBlocked, ref passed);

// 17-19: controlled reevaluation supersedes the prior decision and preserves history.
var reevaluated = await service.ReevaluateAsync(new ReevaluateDisputeDecisionCommand(idempotentInvestigationId, actor, "New evidence submitted"));
Check("reevaluation produces new decision id", reevaluated.DecisionId != first.DecisionId, ref passed);

var supersededOriginal = await service.GetAsync(first.DecisionId);
Check("prior decision superseded", supersededOriginal.Status == ResolutionDecisionStatus.Superseded, ref passed);

var activeAfterReevaluation = await repository.GetActiveByInvestigationAsync(idempotentInvestigationId);
Check("only the reevaluated decision is active", activeAfterReevaluation is not null && activeAfterReevaluation.DecisionId == reevaluated.DecisionId, ref passed);

// 20: get on missing decision rejected.
var getMissingBlocked = false;
try
{
    await service.GetAsync(Guid.NewGuid());
}
catch (KeyNotFoundException)
{
    getMissingBlocked = true;
}
Check("get blocked for missing decision", getMissingBlocked, ref passed);

// 21-23: audit trail and financial boundary proofs.
var events = await audit.GetAsync(unauthorizedLow.DecisionId);
Check("audit trail exists", events.Count > 0, ref passed);
Check("refund execution absent", events.All(x => x.Metadata["refundExecuted"] == "false"), ref passed);
Check("chargeback execution absent", events.All(x => x.Metadata["chargebackExecuted"] == "false"), ref passed);
Check("money movement absent", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);
Check("ledger mutation absent", events.All(x => x.Metadata["ledgerMutationPerformed"] == "false"), ref passed);

var reevaluationEvents = await audit.GetAsync(reevaluated.DecisionId);
Check("reevaluation reason captured in audit", reevaluationEvents.Any(x => x.Metadata.ContainsKey("reevaluationReason")), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0018.4 dispute decision scenarios passed.");
Console.WriteLine("Refund decision: IMPLEMENTED");
Console.WriteLine("Chargeback decision: IMPLEMENTED");
Console.WriteLine("Refund execution: NOT IMPLEMENTED");
Console.WriteLine("Chargeback execution: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IDisputeDecisionClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
