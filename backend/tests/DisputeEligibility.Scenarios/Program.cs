using AfriWallet.Disputes.Eligibility.Application.Abstractions;
using AfriWallet.Disputes.Eligibility.Application.Policies;
using AfriWallet.Disputes.Eligibility.Application.Services;
using AfriWallet.Disputes.Eligibility.Domain.Claims;
using AfriWallet.Disputes.Eligibility.Domain.Classification;
using AfriWallet.Disputes.Eligibility.Domain.Eligibility;
using AfriWallet.Disputes.Eligibility.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-52} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);
var claims = new SandboxDisputeClaimReader();
var transactions = new SandboxTransactionReferenceReader();
var repository = new InMemoryDisputeEligibilityRepository();
var audit = new InMemoryDisputeEligibilityAuditStore();
var service = new DisputeEligibilityService(
    claims, transactions, repository, audit, new FixedClock(now),
    new DisputeEligibilityPolicy(), new DisputeClassificationPolicy());

Guid Seed(
    DisputeClaimType type,
    long claimAmount = 50_000,
    string claimCurrency = "USD",
    long txAmount = 50_000,
    string txCurrency = "USD",
    string txStatus = "Completed",
    int daysAfterTransaction = 10,
    string claimAwid = "AWID-DSP",
    string txAwid = "AWID-DSP",
    bool withTransaction = true)
{
    var claimId = Guid.NewGuid();
    var transactionId = Guid.NewGuid();
    var occurredAt = now.AddDays(-daysAfterTransaction);

    claims.Set(new DisputeClaimSnapshot(
        claimId, claimAwid, transactionId, type, claimAmount, claimCurrency,
        "Sandbox dispute claim.", occurredAt.AddDays(daysAfterTransaction), DisputeChannel.Mobile));

    if (withTransaction)
        transactions.Set(new TransactionReferenceSnapshot(transactionId, txAwid, txAmount, txCurrency, txStatus, occurredAt));

    return claimId;
}

var eligibleClaimId = Seed(DisputeClaimType.TransactionNotRecognized);
var eligible = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(eligibleClaimId, "scenario-runner"));
Check("eligible decision created", eligible.DecisionId != Guid.Empty, ref passed);
Check("eligible status resolved", eligible.Status == DisputeEligibilityStatus.Eligible, ref passed);
Check("eligible primary reason", eligible.PrimaryReason == DisputeEligibilityReason.Eligible, ref passed);
Check("unauthorized classification", eligible.Classification.Category == DisputeCategory.UnauthorizedTransaction, ref passed);
Check("all eligibility rules evaluated", eligible.Rules.Count == 6 && eligible.Rules.All(x => x.Passed), ref passed);
Check("rule evidence explainable", eligible.Rules.All(x => !string.IsNullOrWhiteSpace(x.RuleCode) && !string.IsNullOrWhiteSpace(x.Reason)), ref passed);

var mismatchAwid = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.DuplicateCharge, claimAwid: "AWID-OTHER"), "scenario-runner"));
Check("awid mismatch ineligible", mismatchAwid.Status == DisputeEligibilityStatus.Ineligible, ref passed);
Check("awid mismatch reason", mismatchAwid.PrimaryReason == DisputeEligibilityReason.AwidMismatch, ref passed);

var mismatchCurrency = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.WrongAmount, claimCurrency: "EUR"), "scenario-runner"));
Check("currency mismatch reason", mismatchCurrency.PrimaryReason == DisputeEligibilityReason.CurrencyMismatch, ref passed);

var amountExceeds = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.WrongAmount, claimAmount: 90_000, txAmount: 50_000), "scenario-runner"));
Check("claim amount exceeds transaction", amountExceeds.PrimaryReason == DisputeEligibilityReason.ClaimAmountExceedsTransaction, ref passed);

var windowExpired = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.GoodsNotReceived, daysAfterTransaction: 200), "scenario-runner"));
Check("submission window expired", windowExpired.PrimaryReason == DisputeEligibilityReason.SubmissionWindowExpired, ref passed);

var notCompleted = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.ServiceNotReceived, txStatus: "Pending"), "scenario-runner"));
Check("transaction not completed", notCompleted.PrimaryReason == DisputeEligibilityReason.TransactionNotCompleted, ref passed);

var settledAccepted = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.CashWithdrawalDispute, txStatus: "Settled"), "scenario-runner"));
Check("settled transaction accepted", settledAccepted.Status == DisputeEligibilityStatus.Eligible, ref passed);

var missingTransaction = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.BankTransferDispute, withTransaction: false), "scenario-runner"));
Check("missing transaction ineligible", missingTransaction.Status == DisputeEligibilityStatus.Ineligible, ref passed);
Check("missing transaction reason", missingTransaction.PrimaryReason == DisputeEligibilityReason.TransactionNotFound, ref passed);

var manual = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(
    Seed(DisputeClaimType.Other), "scenario-runner"));
Check("other claim requires manual review", manual.Status == DisputeEligibilityStatus.ManualReviewRequired, ref passed);
Check("manual review reason", manual.PrimaryReason == DisputeEligibilityReason.ManualReviewRequired, ref passed);
Check("other classification preserved", manual.Classification.Category == DisputeCategory.Other, ref passed);

var processingError = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(Seed(DisputeClaimType.DuplicateCharge), "scenario-runner"));
Check("duplicate charge classified", processingError.Classification.Category == DisputeCategory.ProcessingError, ref passed);
var merchantService = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(Seed(DisputeClaimType.MerchantDispute), "scenario-runner"));
Check("merchant dispute classified", merchantService.Classification.Category == DisputeCategory.MerchantService, ref passed);
var refundIssue = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(Seed(DisputeClaimType.RefundNotReceived), "scenario-runner"));
Check("refund issue classified", refundIssue.Classification.Category == DisputeCategory.RefundIssue, ref passed);
Check("cash withdrawal classified", settledAccepted.Classification.Category == DisputeCategory.CashWithdrawal, ref passed);
Check("bank transfer classified", missingTransaction.Classification.Category == DisputeCategory.BankTransfer, ref passed);
var fraudRelated = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(Seed(DisputeClaimType.FraudRelated), "scenario-runner"));
Check("fraud related classified", fraudRelated.Classification.Category == DisputeCategory.FraudRelated, ref passed);
Check("fraud context is not a dispute decision", fraudRelated.Status == DisputeEligibilityStatus.Eligible, ref passed);

var repeat = await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(eligibleClaimId, "scenario-runner"));
Check("evaluation is deterministic", repeat.Status == eligible.Status && repeat.Classification == eligible.Classification, ref passed);

var stored = await repository.GetByClaimAsync(eligibleClaimId);
Check("eligibility decision persisted", stored is not null && stored.ClaimId == eligibleClaimId, ref passed);

var events = await audit.GetAsync(eligible.DecisionId);
Check("eligibility audit recorded", events.Count == 1, ref passed);
Check("refund decision not performed", events.Single().Metadata["refundDecisionPerformed"] == "false", ref passed);
Check("chargeback not performed", events.Single().Metadata["chargebackPerformed"] == "false", ref passed);
Check("money movement not performed", events.Single().Metadata["moneyMovementPerformed"] == "false", ref passed);

var missingClaimBlocked = false;
try
{
    await service.EvaluateAsync(new EvaluateDisputeEligibilityCommand(Guid.NewGuid(), "scenario-runner"));
}
catch (KeyNotFoundException)
{
    missingClaimBlocked = true;
}
Check("missing claim rejected", missingClaimBlocked, ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0018.2 dispute eligibility scenarios passed.");
Console.WriteLine("Refund decision: NOT IMPLEMENTED");
Console.WriteLine("Chargeback execution: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IDisputeEligibilityClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
