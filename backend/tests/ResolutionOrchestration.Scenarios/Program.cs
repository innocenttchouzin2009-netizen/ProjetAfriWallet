using AfriWallet.Disputes.Resolution.Application.Abstractions;
using AfriWallet.Disputes.Resolution.Application.Commands;
using AfriWallet.Disputes.Resolution.Application.Policies;
using AfriWallet.Disputes.Resolution.Application.Services;
using AfriWallet.Disputes.Resolution.Domain.Resolutions;
using AfriWallet.Disputes.Resolution.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";

var decisions = new SandboxDisputeDecisionReader();
var repository = new InMemoryResolutionRepository();
var provider = new SandboxResolutionProvider();
var audit = new InMemoryResolutionAuditStore();
var service = new ResolutionOrchestrationService(
    repository, decisions, provider, audit, new FixedClock(now), new ResolutionRoutingPolicy(), new ResolutionRetryPolicy());

Guid SeedDecision(string decisionType, string status = "Approved")
{
    var decisionId = Guid.NewGuid();
    decisions.Set(new DisputeDecisionSnapshot(
        decisionId, Guid.NewGuid(), Guid.NewGuid(), "AWID-DSP", decisionType, status, "AFW-DISPUTE-RESOLUTION:1.0", now.AddDays(-1)));
    return decisionId;
}

// 1-3: creation gating.
var missingDecisionBlocked = false;
try
{
    await service.CreateAsync(new CreateResolutionCommand(Guid.NewGuid(), "idem-missing", actor));
}
catch (KeyNotFoundException)
{
    missingDecisionBlocked = true;
}
Check("create blocked for missing decision", missingDecisionBlocked, ref passed);

var notApprovedBlocked = false;
try
{
    await service.CreateAsync(new CreateResolutionCommand(
        SeedDecision("RefundRecommended", status: "PendingManualApproval"), "idem-not-approved", actor));
}
catch (InvalidOperationException)
{
    notApprovedBlocked = true;
}
Check("create blocked for unapproved decision", notApprovedBlocked, ref passed);

var unsupportedTypeBlocked = false;
try
{
    await service.CreateAsync(new CreateResolutionCommand(
        SeedDecision("ManualReview"), "idem-unsupported", actor));
}
catch (InvalidOperationException)
{
    unsupportedTypeBlocked = true;
}
Check("create blocked for unsupported decision type", unsupportedTypeBlocked, ref passed);

// 4-5: route selection.
var refundDecisionId = SeedDecision("RefundRecommended");
var refund = await service.CreateAsync(new CreateResolutionCommand(refundDecisionId, "idem-refund-001", actor));
Check("refund route selected", refund.Route == ResolutionRoute.Refund, ref passed);
Check("resolution starts created", refund.Status == ResolutionStatus.Created, ref passed);

var chargebackDecisionId = SeedDecision("ChargebackRecommended");
var chargeback = await service.CreateAsync(new CreateResolutionCommand(chargebackDecisionId, "idem-chargeback-001", actor));
Check("chargeback route selected", chargeback.Route == ResolutionRoute.Chargeback, ref passed);

// 6-7: idempotent creation.
var refundAgainSameKey = await service.CreateAsync(new CreateResolutionCommand(refundDecisionId, "idem-refund-001", actor));
Check("idempotent creation same key returns same id", refundAgainSameKey.ResolutionId == refund.ResolutionId, ref passed);

var refundAgainDifferentKey = await service.CreateAsync(new CreateResolutionCommand(refundDecisionId, "idem-refund-002", actor));
Check("idempotent creation same decision returns same id", refundAgainDifferentKey.ResolutionId == refund.ResolutionId, ref passed);

// 8: successful dispatch acknowledges the resolution.
provider.Enqueue(ProviderSubmissionStatus.Accepted);
var dispatchedRefund = await service.DispatchAsync(new DispatchResolutionCommand(refund.ResolutionId, actor));
Check("dispatch acknowledges resolution", dispatchedRefund.Status == ResolutionStatus.Acknowledged, ref passed);
Check("dispatch records attempt", dispatchedRefund.AttemptCount == 1, ref passed);
Check("dispatch stores provider reference", !string.IsNullOrWhiteSpace(dispatchedRefund.ProviderReference), ref passed);

// 9: dispatch not eligible once already dispatched.
var redispatchBlocked = false;
try
{
    await service.DispatchAsync(new DispatchResolutionCommand(refund.ResolutionId, actor));
}
catch (InvalidOperationException)
{
    redispatchBlocked = true;
}
Check("re-dispatch blocked once acknowledged", redispatchBlocked, ref passed);

// 10: partial failure requires compensation.
provider.Enqueue(ProviderSubmissionStatus.PartialFailure);
var dispatchedChargeback = await service.DispatchAsync(new DispatchResolutionCommand(chargeback.ResolutionId, actor));
Check("partial failure requires compensation", dispatchedChargeback.Status == ResolutionStatus.CompensationRequired, ref passed);
Check("compensation record created", dispatchedChargeback.CompensationCount == 1, ref passed);

// 11-12: compensation workflow.
var compensated = await service.CompensateAsync(new CompensateResolutionCommand(chargeback.ResolutionId, actor));
Check("compensation completes resolution", compensated.Status == ResolutionStatus.Compensated, ref passed);

var compensateNotRequiredBlocked = false;
try
{
    await service.CompensateAsync(new CompensateResolutionCommand(refund.ResolutionId, actor));
}
catch (InvalidOperationException)
{
    compensateNotRequiredBlocked = true;
}
Check("compensation blocked when not required", compensateNotRequiredBlocked, ref passed);

// 13-15: resolution completion.
var resolvedFromAcknowledged = await service.ResolveAsync(new ResolveResolutionCommand(refund.ResolutionId, actor));
Check("resolve from acknowledged", resolvedFromAcknowledged.Status == ResolutionStatus.Resolved, ref passed);

var resolvedFromCompensated = await service.ResolveAsync(new ResolveResolutionCommand(chargeback.ResolutionId, actor));
Check("resolve from compensated", resolvedFromCompensated.Status == ResolutionStatus.Resolved, ref passed);

var freshDecisionId = SeedDecision("RefundRecommended");
var freshResolution = await service.CreateAsync(new CreateResolutionCommand(freshDecisionId, "idem-fresh-001", actor));
var resolveIneligibleBlocked = false;
try
{
    await service.ResolveAsync(new ResolveResolutionCommand(freshResolution.ResolutionId, actor));
}
catch (InvalidOperationException)
{
    resolveIneligibleBlocked = true;
}
Check("resolve blocked from created state", resolveIneligibleBlocked, ref passed);

// 16-17: retry policy exhaustion after repeated temporary failures.
var retryDecisionId = SeedDecision("RefundRecommended");
var retryResolution = await service.CreateAsync(new CreateResolutionCommand(retryDecisionId, "idem-retry-001", actor));

provider.Enqueue(ProviderSubmissionStatus.TemporaryFailure);
var afterFirstFailure = await service.DispatchAsync(new DispatchResolutionCommand(retryResolution.ResolutionId, actor));
Check("first temporary failure schedules retry", afterFirstFailure.Status == ResolutionStatus.RetryPending, ref passed);

provider.Enqueue(ProviderSubmissionStatus.TemporaryFailure);
var afterSecondFailure = await service.RetryAsync(new RetryResolutionCommand(retryResolution.ResolutionId, actor));
Check("second temporary failure still retries", afterSecondFailure.Status == ResolutionStatus.RetryPending, ref passed);

provider.Enqueue(ProviderSubmissionStatus.TemporaryFailure);
var afterThirdFailure = await service.RetryAsync(new RetryResolutionCommand(retryResolution.ResolutionId, actor));
Check("retry exhaustion requires manual intervention", afterThirdFailure.Status == ResolutionStatus.ManualInterventionRequired, ref passed);
Check("retry exhaustion reason recorded", afterThirdFailure.ReasonCode == ResolutionReasonCode.RetryExhausted, ref passed);

// 18-19: permanent failure and terminal-state immutability.
var permanentFailureDecisionId = SeedDecision("ChargebackRecommended");
var permanentFailureResolution = await service.CreateAsync(new CreateResolutionCommand(permanentFailureDecisionId, "idem-permanent-001", actor));
provider.Enqueue(ProviderSubmissionStatus.PermanentFailure);
var failed = await service.DispatchAsync(new DispatchResolutionCommand(permanentFailureResolution.ResolutionId, actor));
Check("permanent failure fails resolution", failed.Status == ResolutionStatus.Failed, ref passed);

var dispatchAfterFailBlocked = false;
try
{
    await service.DispatchAsync(new DispatchResolutionCommand(permanentFailureResolution.ResolutionId, actor));
}
catch (InvalidOperationException)
{
    dispatchAfterFailBlocked = true;
}
Check("dispatch blocked after terminal failure", dispatchAfterFailBlocked, ref passed);

// 20-21: missing resolution rejected.
var getMissingBlocked = false;
try
{
    await service.GetAsync(Guid.NewGuid());
}
catch (KeyNotFoundException)
{
    getMissingBlocked = true;
}
Check("get blocked for missing resolution", getMissingBlocked, ref passed);

var dispatchMissingBlocked = false;
try
{
    await service.DispatchAsync(new DispatchResolutionCommand(Guid.NewGuid(), actor));
}
catch (KeyNotFoundException)
{
    dispatchMissingBlocked = true;
}
Check("dispatch blocked for missing resolution", dispatchMissingBlocked, ref passed);

// 22: persistence.
var stored = await repository.GetAsync(refund.ResolutionId);
Check("resolution persisted", stored is not null && stored.ResolutionId == refund.ResolutionId, ref passed);

// 23-24: audit trail and financial boundary proofs.
var events = await audit.GetAsync(refund.ResolutionId);
Check("resolution audit exists", events.Count >= 3, ref passed);
Check("real refund absent", events.All(x => x.Metadata["realRefundPerformed"] == "false"), ref passed);
Check("real chargeback absent", events.All(x => x.Metadata["realChargebackSubmitted"] == "false"), ref passed);
Check("real money movement absent", events.All(x => x.Metadata["realMoneyMovementPerformed"] == "false"), ref passed);
Check("direct ledger mutation absent", events.All(x => x.Metadata["directLedgerMutationPerformed"] == "false"), ref passed);
Check("external settlement absent", events.All(x => x.Metadata["externalProviderSettlementPerformed"] == "false"), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0018.5 resolution orchestration scenarios passed.");
Console.WriteLine("Resolution orchestration: IMPLEMENTED");
Console.WriteLine("Refund routing: IMPLEMENTED");
Console.WriteLine("Chargeback routing: IMPLEMENTED");
Console.WriteLine("Retry policy: IMPLEMENTED");
Console.WriteLine("Compensation workflow: IMPLEMENTED");
Console.WriteLine("Real refund execution: NOT IMPLEMENTED");
Console.WriteLine("Real chargeback submission: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Direct ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IResolutionClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
