using AfriWallet.Merchants.Onboarding.Application.Abstractions;
using AfriWallet.Merchants.Onboarding.Application.Commands;
using AfriWallet.Merchants.Onboarding.Application.Policies;
using AfriWallet.Merchants.Onboarding.Application.Services;
using AfriWallet.Merchants.Onboarding.Domain.Cases;
using AfriWallet.Merchants.Onboarding.Domain.Documents;
using AfriWallet.Merchants.Onboarding.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-58} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2027, 2, 3, 9, 0, 0, TimeSpan.Zero);
const string actor = "scenario-runner";

var profiles = new SandboxMerchantProfileReader();
var repository = new InMemoryMerchantVerificationRepository();
var provider = new SandboxMerchantVerificationProvider();
var audit = new InMemoryMerchantVerificationAuditStore();
var service = new MerchantVerificationService(repository, profiles, provider, audit, new FixedClock(now), new MerchantVerificationPolicy());

string SeedMerchant(string merchantId, string ownerAwid)
{
    profiles.Set(new MerchantProfileSnapshot(merchantId, ownerAwid, "Active", "Sandbox Legal SARL", "CI", "XOF"));
    return merchantId;
}

int docCounter = 0;
Task<AfriWallet.Merchants.Onboarding.Application.Results.MerchantVerificationResult> AddDoc(Guid verificationId, VerificationDocumentType type) =>
    service.AddDocumentAsync(new AddVerificationDocumentCommand(
        verificationId, type, $"ref-{++docCounter}", $"sha-{docCounter:D6}", 1024, "application/pdf", "merchant-owner", actor));

// 1-3: creation gating and idempotency.
var missingMerchantBlocked = false;
try
{
    await service.CreateAsync(new CreateVerificationCommand("AFM-MISSING", actor));
}
catch (KeyNotFoundException)
{
    missingMerchantBlocked = true;
}
Check("create blocked for missing merchant", missingMerchantBlocked, ref passed);

SeedMerchant("AFM-PRIMARY", "AWID-M-001");
var primary = await service.CreateAsync(new CreateVerificationCommand("AFM-PRIMARY", actor));
Check("verification created", primary.Status == MerchantVerificationStatus.Created, ref passed);
Check("verification starts with no documents", primary.DocumentCount == 0, ref passed);

var primaryAgain = await service.CreateAsync(new CreateVerificationCommand("AFM-PRIMARY", actor));
Check("create is idempotent per merchant", primaryAgain.VerificationId == primary.VerificationId, ref passed);

// 4-7: document collection and auto-transition to ready-for-review.
var withFirstDoc = await AddDoc(primary.VerificationId, VerificationDocumentType.BusinessRegistration);
Check("first document moves to pending documents", withFirstDoc.Status == MerchantVerificationStatus.PendingDocuments, ref passed);

var duplicateHashBlocked = false;
try
{
    await service.AddDocumentAsync(new AddVerificationDocumentCommand(
        primary.VerificationId, VerificationDocumentType.Other, "ref-dup", $"sha-{docCounter:D6}", 10, "text/plain", "merchant-owner", actor));
}
catch (InvalidOperationException)
{
    duplicateHashBlocked = true;
}
Check("duplicate document hash rejected", duplicateHashBlocked, ref passed);

var withSecondDoc = await AddDoc(primary.VerificationId, VerificationDocumentType.ProofOfAddress);
Check("second document still pending", withSecondDoc.Status == MerchantVerificationStatus.PendingDocuments, ref passed);

var withThirdDoc = await AddDoc(primary.VerificationId, VerificationDocumentType.OwnerIdentity);
Check("minimum document set moves to ready for review", withThirdDoc.Status == MerchantVerificationStatus.ReadyForReview, ref passed);
Check("document count recorded", withThirdDoc.DocumentCount == 3, ref passed);

// 8-10: reviewer assignment.
SeedMerchant("AFM-SECONDARY", "AWID-M-002");
var secondary = await service.CreateAsync(new CreateVerificationCommand("AFM-SECONDARY", actor));
var assignBeforeReadyBlocked = false;
try
{
    await service.AssignReviewerAsync(new AssignVerificationReviewerCommand(secondary.VerificationId, "reviewer-1", actor));
}
catch (InvalidOperationException)
{
    assignBeforeReadyBlocked = true;
}
Check("assignment blocked before ready for review", assignBeforeReadyBlocked, ref passed);

var assigned = await service.AssignReviewerAsync(new AssignVerificationReviewerCommand(primary.VerificationId, "reviewer-1", actor));
Check("reviewer assigned", assigned.AssignedReviewer == "reviewer-1", ref passed);
Check("assignment moves to under review", assigned.Status == MerchantVerificationStatus.UnderReview, ref passed);

var reassignBlocked = false;
try
{
    await service.AssignReviewerAsync(new AssignVerificationReviewerCommand(primary.VerificationId, "reviewer-2", actor));
}
catch (InvalidOperationException)
{
    reassignBlocked = true;
}
Check("re-assignment blocked once under review", reassignBlocked, ref passed);

// 11: review notes.
var withNote = await service.AddNoteAsync(new AddVerificationNoteCommand(primary.VerificationId, "Documents look consistent.", actor));
Check("review note recorded", withNote.NoteCount == 1, ref passed);

// 12-14: execution outcomes (verified, rejected, manual review required).
var verifiedResult = await service.ExecuteAsync(new ExecuteVerificationCommand(primary.VerificationId, actor));
Check("execution verifies merchant", verifiedResult.Status == MerchantVerificationStatus.Verified, ref passed);
Check("verified decision recorded", verifiedResult.Decision == MerchantVerificationDecision.Verified, ref passed);

async Task<Guid> BuildReadyCase(string merchantId, string ownerAwid)
{
    SeedMerchant(merchantId, ownerAwid);
    var created = await service.CreateAsync(new CreateVerificationCommand(merchantId, actor));
    await AddDoc(created.VerificationId, VerificationDocumentType.BusinessRegistration);
    await AddDoc(created.VerificationId, VerificationDocumentType.ProofOfAddress);
    await AddDoc(created.VerificationId, VerificationDocumentType.OwnerIdentity);
    await service.AssignReviewerAsync(new AssignVerificationReviewerCommand(created.VerificationId, "reviewer-1", actor));
    return created.VerificationId;
}

var rejectedId = await BuildReadyCase("AFM-REJECTED", "AWID-M-003");
provider.Enqueue(VerificationProviderDecision.Rejected);
var rejectedResult = await service.ExecuteAsync(new ExecuteVerificationCommand(rejectedId, actor));
Check("execution rejects merchant", rejectedResult.Status == MerchantVerificationStatus.Rejected, ref passed);

var manualReviewId = await BuildReadyCase("AFM-MANUAL", "AWID-M-004");
provider.Enqueue(VerificationProviderDecision.ManualReviewRequired);
var manualReviewResult = await service.ExecuteAsync(new ExecuteVerificationCommand(manualReviewId, actor));
Check("execution requires manual review", manualReviewResult.Status == MerchantVerificationStatus.ManualReviewRequired, ref passed);
Check("manual review decision recorded", manualReviewResult.Decision == MerchantVerificationDecision.ManualReviewRequired, ref passed);

// 15: execution blocked before under review.
var executeBeforeReviewBlocked = false;
try
{
    await service.ExecuteAsync(new ExecuteVerificationCommand(secondary.VerificationId, actor));
}
catch (InvalidOperationException)
{
    executeBeforeReviewBlocked = true;
}
Check("execution blocked before under review", executeBeforeReviewBlocked, ref passed);

// 16-19: terminal-state immutability (domain-level, closure workflow).
var storedVerified = await repository.GetAsync(primary.VerificationId);
Check("verified case persisted", storedVerified is not null && storedVerified.Status == MerchantVerificationStatus.Verified, ref passed);

storedVerified!.Close(now);
Check("verified case can be closed", storedVerified.Status == MerchantVerificationStatus.Closed, ref passed);

var closeNonTerminalBlocked = false;
try
{
    var storedManual = await repository.GetAsync(manualReviewId);
    storedManual!.Close(now);
}
catch (InvalidOperationException)
{
    closeNonTerminalBlocked = true;
}
Check("close blocked for non-terminal decision", closeNonTerminalBlocked, ref passed);

var mutateAfterCloseBlocked = false;
try
{
    await service.AddNoteAsync(new AddVerificationNoteCommand(primary.VerificationId, "Late note.", actor));
}
catch (InvalidOperationException)
{
    mutateAfterCloseBlocked = true;
}
Check("mutation blocked after closure", mutateAfterCloseBlocked, ref passed);

// 20-21: missing verification rejected.
var getMissingBlocked = false;
try
{
    await service.GetAsync(Guid.NewGuid());
}
catch (KeyNotFoundException)
{
    getMissingBlocked = true;
}
Check("get blocked for missing verification", getMissingBlocked, ref passed);

var addDocMissingBlocked = false;
try
{
    await AddDoc(Guid.NewGuid(), VerificationDocumentType.Other);
}
catch (KeyNotFoundException)
{
    addDocMissingBlocked = true;
}
Check("add document blocked for missing verification", addDocMissingBlocked, ref passed);

// 22: persistence check for a distinct case.
var storedRejected = await repository.GetAsync(rejectedId);
Check("rejected case persisted", storedRejected is not null && storedRejected.Status == MerchantVerificationStatus.Rejected, ref passed);

// 23-29: audit trail and boundary proofs.
var events = await audit.GetAsync(primary.VerificationId);
Check("audit trail exists", events.Count >= 1, ref passed);
Check("sandbox verification flagged", events.All(x => x.Metadata["sandboxVerification"] == "true"), ref passed);
Check("payment acceptance not enabled", events.All(x => x.Metadata["paymentAcceptanceEnabled"] == "false"), ref passed);
Check("capture not enabled", events.All(x => x.Metadata["captureEnabled"] == "false"), ref passed);
Check("settlement not enabled", events.All(x => x.Metadata["settlementEnabled"] == "false"), ref passed);
Check("payout not enabled", events.All(x => x.Metadata["payoutEnabled"] == "false"), ref passed);
Check("money movement absent", events.All(x => x.Metadata["moneyMovementPerformed"] == "false"), ref passed);
Check("ledger mutation absent", events.All(x => x.Metadata["ledgerMutationPerformed"] == "false"), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0019.2 merchant verification scenarios passed.");
Console.WriteLine("Merchant onboarding: IMPLEMENTED");
Console.WriteLine("Sandbox merchant verification: IMPLEMENTED");
Console.WriteLine("Payment acceptance: NOT IMPLEMENTED");
Console.WriteLine("Payment capture: NOT IMPLEMENTED");
Console.WriteLine("Settlement: NOT IMPLEMENTED");
Console.WriteLine("Payout: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IMerchantVerificationClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
