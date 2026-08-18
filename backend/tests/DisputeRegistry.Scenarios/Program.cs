using AfriWallet.Disputes.Registry.Application.Abstractions;
using AfriWallet.Disputes.Registry.Application.Claims;
using AfriWallet.Disputes.Registry.Domain.Claims;
using AfriWallet.Disputes.Registry.Domain.Evidence;
using AfriWallet.Disputes.Registry.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-52} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 8, 18, 9, 0, 0, TimeSpan.Zero);
var repository = new InMemoryDisputeClaimRepository();
var audit = new InMemoryDisputeRegistryAuditStore();
var service = new DisputeRegistryService(repository, audit, new FixedClock(now));

var transactionId = Guid.NewGuid();
var registered = await service.RegisterAsync(new RegisterDisputeClaimCommand(
    "AWID-DISPUTE",
    transactionId,
    DisputeClaimType.TransactionNotRecognized,
    "Customer does not recognize this debit.",
    250_000,
    "usd",
    "Card debit of 2500.00 was not authorized by the customer.",
    DisputeSourceChannel.MobileApp,
    "PAY-REF-001",
    null,
    "MERCHANT-REF-77",
    "scenario-runner"));

Check("claim registered as draft", registered.Status == DisputeClaimStatus.Draft, ref passed);
Check("claim identity preserved", registered.Awid == "AWID-DISPUTE" && registered.TransactionId == transactionId, ref passed);
Check("claim amount normalized", registered.AmountMinor == 250_000 && registered.Currency == "USD", ref passed);
Check("draft has no submission timestamp", registered.SubmittedAtUtc is null, ref passed);

var submitted = await service.SubmitAsync(registered.ClaimId, "customer-001");
Check("claim submitted", submitted.Status == DisputeClaimStatus.Submitted, ref passed);
Check("submission timestamp recorded", submitted.SubmittedAtUtc == now, ref passed);

var opened = await service.OpenAsync(registered.ClaimId, "dispute-officer");
Check("claim opened", opened.Status == DisputeClaimStatus.Open, ref passed);

var reviewing = await service.StartReviewAsync(registered.ClaimId, "dispute-officer");
Check("review started", reviewing.Status == DisputeClaimStatus.UnderReview, ref passed);

var withReceipt = await service.LinkEvidenceAsync(new LinkDisputeEvidenceCommand(
    registered.ClaimId, DisputeEvidenceType.Receipt, "RECEIPT-9001", "Customer receipt provided.", "dispute-officer"));
Check("evidence reference linked", withReceipt.EvidenceCount == 1, ref passed);

var withFraud = await service.LinkEvidenceAsync(new LinkDisputeEvidenceCommand(
    registered.ClaimId, DisputeEvidenceType.FraudFinding, "FRAUD-FINDING-4412", "Fraud intelligence finding referenced.", "dispute-officer"));
Check("fraud finding stored as evidence only", withFraud.EvidenceCount == 2 && withFraud.Status == DisputeClaimStatus.UnderReview, ref passed);

var resolved = await service.ResolveAsync(new ResolveDisputeClaimCommand(
    registered.ClaimId, "Investigation complete; outcome recorded for downstream decisioning.", "dispute-officer"));
Check("claim resolved with outcome", resolved.Status == DisputeClaimStatus.Resolved && !string.IsNullOrWhiteSpace(resolved.Outcome), ref passed);

var closed = await service.CloseAsync(registered.ClaimId, "dispute-officer");
Check("claim closed", closed.Status == DisputeClaimStatus.Closed, ref passed);
Check("lifecycle history recorded", closed.HistoryCount == 5, ref passed);

var closedImmutable = false;
try
{
    await service.LinkEvidenceAsync(new LinkDisputeEvidenceCommand(
        registered.ClaimId, DisputeEvidenceType.AnalystNote, "NOTE-1", "Must be rejected.", "dispute-officer"));
}
catch (InvalidOperationException)
{
    closedImmutable = true;
}
Check("closed claim immutable", closedImmutable, ref passed);

var rejectedClaim = await service.RegisterAsync(new RegisterDisputeClaimCommand(
    "AWID-DISPUTE", Guid.NewGuid(), DisputeClaimType.DuplicateCharge, "Possible duplicate charge.",
    12_000, "USD", "Customer reports a duplicate debit.", DisputeSourceChannel.CallCenter, null, null, null, "scenario-runner"));
await service.SubmitAsync(rejectedClaim.ClaimId, "customer-002");
var rejected = await service.RejectAsync(new RejectDisputeClaimCommand(
    rejectedClaim.ClaimId, "Transaction was authenticated and already refunded previously.", "dispute-officer"));
Check("claim rejection recorded", rejected.Status == DisputeClaimStatus.Rejected && rejected.Outcome is not null, ref passed);

var cancelledClaim = await service.RegisterAsync(new RegisterDisputeClaimCommand(
    "AWID-DISPUTE", Guid.NewGuid(), DisputeClaimType.GoodsNotReceived, "Goods not delivered.",
    45_000, "EUR", "Customer opened a claim then received the goods.", DisputeSourceChannel.WebPortal, null, null, "MERCHANT-REF-12", "scenario-runner"));
var cancelled = await service.CancelAsync(new CancelDisputeClaimCommand(
    cancelledClaim.ClaimId, "Customer withdrew the claim.", "customer-003"));
Check("claim cancellation recorded", cancelled.Status == DisputeClaimStatus.Cancelled, ref passed);

var invalidTransitionBlocked = false;
try
{
    await service.ResolveAsync(new ResolveDisputeClaimCommand(cancelledClaim.ClaimId, "Should not apply.", "dispute-officer"));
}
catch (InvalidOperationException)
{
    invalidTransitionBlocked = true;
}
Check("invalid transition blocked", invalidTransitionBlocked, ref passed);

var missingClaimBlocked = false;
try
{
    await service.SubmitAsync(Guid.NewGuid(), "dispute-officer");
}
catch (InvalidOperationException)
{
    missingClaimBlocked = true;
}
Check("unknown claim blocked", missingClaimBlocked, ref passed);

var byAwid = await service.GetByAwidAsync("AWID-DISPUTE");
Check("claims queryable by AWID", byAwid.Count == 3, ref passed);

var events = await audit.GetByClaimAsync(registered.ClaimId);
Check("audit trail recorded", events.Count == 8, ref passed);
Check("audit proves no refund decision", events.All(x => x.Metadata["refundDecisionPerformed"] == "false"), ref passed);
Check("audit proves no money movement", events.All(x => x.Metadata["moneyMovementPerformed"] == "false" && x.Metadata["chargebackPerformed"] == "false"), ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0018.1 dispute claim registry scenarios passed.");
Console.WriteLine("Refund decision: NOT IMPLEMENTED");
Console.WriteLine("Chargeback execution: NOT IMPLEMENTED");
Console.WriteLine("Ledger mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IDisputeRegistryClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
