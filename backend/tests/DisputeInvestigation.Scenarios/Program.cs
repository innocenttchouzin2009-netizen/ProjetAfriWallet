using AfriWallet.Disputes.Investigation.Application.Abstractions;
using AfriWallet.Disputes.Investigation.Application.Commands;
using AfriWallet.Disputes.Investigation.Application.Services;
using AfriWallet.Disputes.Investigation.Domain.Cases;
using AfriWallet.Disputes.Investigation.Domain.Evidence;
using AfriWallet.Disputes.Investigation.Domain.Requests;
using AfriWallet.Disputes.Investigation.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";

var eligibility = new SandboxDisputeEligibilityReader();
var repository = new InMemoryDisputeInvestigationRepository();
var audit = new InMemoryDisputeInvestigationAuditStore();
var service = new DisputeInvestigationService(repository, eligibility, audit, new FixedClock(now));

Guid SeedEligibility(string status)
{
    var claimId = Guid.NewGuid();
    eligibility.Set(new DisputeEligibilitySnapshot(Guid.NewGuid(), claimId, "AWID-DSP", status, "UnauthorizedTransaction", now.AddDays(-1)));
    return claimId;
}

// 1-2: creation gating on missing / ineligible eligibility decisions.
var missingClaimBlocked = false;
try
{
    await service.CreateAsync(new CreateInvestigationCommand(Guid.NewGuid(), actor));
}
catch (InvalidOperationException)
{
    missingClaimBlocked = true;
}
Check("create blocked without eligibility decision", missingClaimBlocked, ref passed);

var ineligibleBlocked = false;
try
{
    await service.CreateAsync(new CreateInvestigationCommand(SeedEligibility("Ineligible"), actor));
}
catch (InvalidOperationException)
{
    ineligibleBlocked = true;
}
Check("create blocked for ineligible claim", ineligibleBlocked, ref passed);

// 3: creation succeeds for an Eligible claim (primary investigation used through the full lifecycle).
var primaryClaimId = SeedEligibility("Eligible");
var primary = await service.CreateAsync(new CreateInvestigationCommand(primaryClaimId, actor));
Check("investigation created", primary.InvestigationId != Guid.Empty, ref passed);
Check("investigation starts open", primary.Status == InvestigationStatus.Open, ref passed);
Check("investigation outcome starts none", primary.Outcome == InvestigationOutcome.None, ref passed);
Check("investigation claim id preserved", primary.ClaimId == primaryClaimId, ref passed);

// 4: creation also succeeds for a ManualReviewRequired claim.
var manualReviewClaimId = SeedEligibility("ManualReviewRequired");
var manualReview = await service.CreateAsync(new CreateInvestigationCommand(manualReviewClaimId, actor));
Check("create allowed for manual review claim", manualReview.Status == InvestigationStatus.Open, ref passed);

// 5-6: evidence cannot be requested or added before assignment.
var requestBeforeAssignBlocked = false;
try
{
    await service.RequestEvidenceAsync(new RequestEvidenceCommand(manualReview.InvestigationId, EvidenceType.MerchantReceipt, "merchant-x", "need receipt", actor));
}
catch (InvalidOperationException)
{
    requestBeforeAssignBlocked = true;
}
Check("evidence request blocked before assignment", requestBeforeAssignBlocked, ref passed);

var addBeforeAssignBlocked = false;
try
{
    await service.AddEvidenceAsync(new AddEvidenceCommand(
        manualReview.InvestigationId, EvidenceType.Document, "ref-1", "desc", "sha-unassigned", 10, "application/pdf", "merchant-x", actor));
}
catch (InvalidOperationException)
{
    addBeforeAssignBlocked = true;
}
Check("evidence add blocked before assignment", addBeforeAssignBlocked, ref passed);

// 7-8: assignment lifecycle.
var assigned = await service.AssignAsync(new AssignInvestigationCommand(primary.InvestigationId, "analyst-1", actor));
Check("assignment sets analyst", assigned.AnalystId == "analyst-1", ref passed);
Check("assignment moves status to assigned", assigned.Status == InvestigationStatus.Assigned, ref passed);

var reassignBlocked = false;
try
{
    await service.AssignAsync(new AssignInvestigationCommand(primary.InvestigationId, "analyst-2", actor));
}
catch (InvalidOperationException)
{
    reassignBlocked = true;
}
Check("re-assignment blocked once assigned", reassignBlocked, ref passed);

// 9-10: evidence request lifecycle.
var withRequest = await service.RequestEvidenceAsync(new RequestEvidenceCommand(primary.InvestigationId, EvidenceType.MerchantReceipt, "merchant-x", "need receipt", actor));
Check("evidence request moves to waiting", withRequest.Status == InvestigationStatus.WaitingForEvidence, ref passed);
Check("evidence request tracked as open", withRequest.OpenEvidenceRequests == 1, ref passed);

// 11-12: evidence submission auto-fulfills matching request and advances to under review.
var withEvidence = await service.AddEvidenceAsync(new AddEvidenceCommand(
    primary.InvestigationId, EvidenceType.MerchantReceipt, "receipt-001", "Merchant receipt scan", "sha-receipt-001", 2048, "image/png", "merchant-x", actor));
Check("matching evidence fulfills open request", withEvidence.OpenEvidenceRequests == 0, ref passed);
Check("investigation advances to under review", withEvidence.Status == InvestigationStatus.UnderReview, ref passed);
Check("evidence count incremented", withEvidence.EvidenceCount == 1, ref passed);

// 13: additional evidence can still be added while under review.
var secondEvidence = await service.AddEvidenceAsync(new AddEvidenceCommand(
    primary.InvestigationId, EvidenceType.CustomerStatement, "statement-001", "Customer statement", "sha-statement-001", 512, "text/plain", "customer-y", actor));
Check("additional evidence accepted during review", secondEvidence.EvidenceCount == 2, ref passed);
Check("status remains under review", secondEvidence.Status == InvestigationStatus.UnderReview, ref passed);

// 14: duplicate evidence hash rejected.
var duplicateBlocked = false;
try
{
    await service.AddEvidenceAsync(new AddEvidenceCommand(
        primary.InvestigationId, EvidenceType.Screenshot, "receipt-001-copy", "Duplicate", "SHA-RECEIPT-001", 2048, "image/png", "merchant-x", actor));
}
catch (InvalidOperationException)
{
    duplicateBlocked = true;
}
Check("duplicate evidence hash rejected", duplicateBlocked, ref passed);

// 15: completion requires a concrete outcome.
var completeNoneBlocked = false;
try
{
    await service.CompleteAsync(new CompleteInvestigationCommand(primary.InvestigationId, InvestigationOutcome.None, actor));
}
catch (InvalidOperationException)
{
    completeNoneBlocked = true;
}
Check("completion requires non-none outcome", completeNoneBlocked, ref passed);

// 16: completion blocked while an evidence request is still open.
var pendingCompletionClaimId = SeedEligibility("Eligible");
var pendingCompletion = await service.CreateAsync(new CreateInvestigationCommand(pendingCompletionClaimId, actor));
await service.AssignAsync(new AssignInvestigationCommand(pendingCompletion.InvestigationId, "analyst-3", actor));
await service.RequestEvidenceAsync(new RequestEvidenceCommand(pendingCompletion.InvestigationId, EvidenceType.DeliveryProof, "carrier-x", "need proof of delivery", actor));
var completeWithOpenRequestBlocked = false;
try
{
    await service.CompleteAsync(new CompleteInvestigationCommand(pendingCompletion.InvestigationId, InvestigationOutcome.InsufficientEvidence, actor));
}
catch (InvalidOperationException)
{
    completeWithOpenRequestBlocked = true;
}
Check("completion blocked with open evidence request", completeWithOpenRequestBlocked, ref passed);

// 17: close blocked before completion.
var closeBeforeCompleteBlocked = false;
try
{
    await service.CloseAsync(pendingCompletion.InvestigationId, actor);
}
catch (InvalidOperationException)
{
    closeBeforeCompleteBlocked = true;
}
Check("close blocked before completion", closeBeforeCompleteBlocked, ref passed);

// 18-19: completion and closure of the primary investigation.
var completed = await service.CompleteAsync(new CompleteInvestigationCommand(primary.InvestigationId, InvestigationOutcome.EvidenceSupportsClaim, actor));
Check("investigation completed", completed.Status == InvestigationStatus.Completed, ref passed);
Check("outcome recorded", completed.Outcome == InvestigationOutcome.EvidenceSupportsClaim, ref passed);

var closed = await service.CloseAsync(primary.InvestigationId, actor);
Check("investigation closed", closed.Status == InvestigationStatus.Closed, ref passed);

// 20-21: terminal state immutability.
var assignAfterCloseBlocked = false;
try
{
    await service.AssignAsync(new AssignInvestigationCommand(primary.InvestigationId, "analyst-4", actor));
}
catch (InvalidOperationException)
{
    assignAfterCloseBlocked = true;
}
Check("assignment blocked after closure", assignAfterCloseBlocked, ref passed);

var addEvidenceAfterCloseBlocked = false;
try
{
    await service.AddEvidenceAsync(new AddEvidenceCommand(
        primary.InvestigationId, EvidenceType.Other, "late-ref", "Too late", "sha-late", 10, "text/plain", "someone", actor));
}
catch (InvalidOperationException)
{
    addEvidenceAfterCloseBlocked = true;
}
Check("evidence add blocked after closure", addEvidenceAfterCloseBlocked, ref passed);

// 22: persistence and domain-level detail checks via direct repository access.
var stored = await repository.GetAsync(primary.InvestigationId);
Check("investigation persisted", stored is not null && stored.InvestigationId == primary.InvestigationId, ref passed);
Check("persisted evidence count matches", stored!.Evidence.Count == 2, ref passed);
Check("persisted evidence request fulfilled", stored.Requests.Single().Status == EvidenceRequestStatus.Fulfilled, ref passed);
// Timeline includes internal transitions (request auto-fulfilled, auto-advance to under review)
// in addition to the 7 explicit lifecycle commands: created, assigned, requested, added x2, completed, closed.
Check("persisted timeline captures full lifecycle", stored.Timeline.Count == 9, ref passed);

// 23: missing investigation is rejected as KeyNotFoundException on both read and write paths.
var missingOnAssignBlocked = false;
try
{
    await service.AssignAsync(new AssignInvestigationCommand(Guid.NewGuid(), "analyst-1", actor));
}
catch (KeyNotFoundException)
{
    missingOnAssignBlocked = true;
}
Check("assign on missing investigation rejected", missingOnAssignBlocked, ref passed);

var missingOnGetBlocked = false;
try
{
    await service.GetAsync(Guid.NewGuid());
}
catch (KeyNotFoundException)
{
    missingOnGetBlocked = true;
}
Check("get on missing investigation rejected", missingOnGetBlocked, ref passed);

// 24: audit trail count and financial-boundary proofs.
var events = await audit.GetAsync(primary.InvestigationId);
Check("audit trail records full lifecycle", events.Count == 7, ref passed);
Check("refund decision never performed", events.All(x => x.Metadata["refundDecisionPerformed"] == "false"), ref passed);
Check("chargeback never performed", events.All(x => x.Metadata["chargebackPerformed"] == "false"), ref passed);
Check("money movement never performed", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0018.3 dispute evidence and investigation scenarios passed.");
Console.WriteLine("Refund decision: NOT IMPLEMENTED");
Console.WriteLine("Chargeback execution: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IDisputeInvestigationClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
