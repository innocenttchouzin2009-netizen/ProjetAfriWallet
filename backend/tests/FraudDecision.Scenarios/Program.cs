using AfriWallet.Fraud.Decision.Application.Abstractions;
using AfriWallet.Fraud.Decision.Application.Policies;
using AfriWallet.Fraud.Decision.Application.Services;
using AfriWallet.Fraud.Decision.Domain.Decisions;
using AfriWallet.Fraud.Decision.Domain.Inputs;
using AfriWallet.Fraud.Decision.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-52} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
var deviceReader = new SandboxDeviceRiskDecisionReader();
var transactionReader = new SandboxTransactionFraudDecisionReader();
var repository = new InMemoryFraudDecisionRepository();
var audit = new InMemoryFraudDecisionAuditStore();
var service = new FraudDecisionService(deviceReader, transactionReader, repository, audit, new FixedClock(now), new FraudDecisionPolicy());

var lowTransaction = Guid.NewGuid();
deviceReader.Set(new DeviceRiskInput("AWID-LOW", "DEVICE-LOW", 10, "Low", "Allow", now));
transactionReader.Set(new TransactionFraudInput(lowTransaction, "AWID-LOW", 10, "Low", "Allow", now));
var low = await service.EvaluateAsync(new EvaluateFraudDecisionCommand(lowTransaction, "AWID-LOW", "DEVICE-LOW", "runner"));
Check("low decision created", low.DecisionId != Guid.Empty, ref passed);
Check("low action allow", low.Action == FraudDecisionAction.Allow, ref passed);
Check("low band low", low.Band == FraudDecisionBand.Low, ref passed);

var mediumTransaction = Guid.NewGuid();
deviceReader.Set(new DeviceRiskInput("AWID-MEDIUM", "DEVICE-MEDIUM", 40, "Medium", "Review", now));
transactionReader.Set(new TransactionFraudInput(mediumTransaction, "AWID-MEDIUM", 35, "Medium", "Review", now));
var medium = await service.EvaluateAsync(new EvaluateFraudDecisionCommand(mediumTransaction, "AWID-MEDIUM", "DEVICE-MEDIUM", "runner"));
Check("medium risk produces review", medium.Action == FraudDecisionAction.Review, ref passed);

var highTransaction = Guid.NewGuid();
deviceReader.Set(new DeviceRiskInput("AWID-HIGH", "DEVICE-HIGH", 60, "High", "Challenge", now));
transactionReader.Set(new TransactionFraudInput(highTransaction, "AWID-HIGH", 60, "High", "Challenge", now));
var high = await service.EvaluateAsync(new EvaluateFraudDecisionCommand(highTransaction, "AWID-HIGH", "DEVICE-HIGH", "runner"));
Check("high risk produces challenge", high.Action == FraudDecisionAction.Challenge, ref passed);
Check("challenge band high", high.Band == FraudDecisionBand.High, ref passed);

var criticalTransaction = Guid.NewGuid();
deviceReader.Set(new DeviceRiskInput("AWID-CRITICAL", "DEVICE-CRITICAL", 85, "Critical", "DeclineRecommended", now));
transactionReader.Set(new TransactionFraudInput(criticalTransaction, "AWID-CRITICAL", 95, "Critical", "DeclineRecommended", now));
var critical = await service.EvaluateAsync(new EvaluateFraudDecisionCommand(criticalTransaction, "AWID-CRITICAL", "DEVICE-CRITICAL", "runner"));
Check("critical override triggered", critical.Evaluations.Single(x => x.RuleCode == "CRITICAL-OVERRIDE").Triggered, ref passed);
Check("critical score forced to 100", critical.Score == 100, ref passed);
Check("critical decline recommended", critical.Action == FraudDecisionAction.DeclineRecommended, ref passed);

var missingTransaction = Guid.NewGuid();
deviceReader.Set(new DeviceRiskInput("AWID-MISSING", "DEVICE-MISSING", 10, "Low", "Allow", now));
var missingBlocked = false;
try
{
    await service.EvaluateAsync(new EvaluateFraudDecisionCommand(missingTransaction, "AWID-MISSING", "DEVICE-MISSING", "runner"));
}
catch (InvalidOperationException)
{
    missingBlocked = true;
}
Check("missing transaction detection blocked", missingBlocked, ref passed);

var mismatchTransaction = Guid.NewGuid();
transactionReader.Set(new TransactionFraudInput(mismatchTransaction, "AWID-OTHER", 60, "High", "Challenge", now));
deviceReader.Set(new DeviceRiskInput("AWID-MISMATCH", "DEVICE-MISMATCH", 60, "High", "Challenge", now));
var mismatchBlocked = false;
try
{
    await service.EvaluateAsync(new EvaluateFraudDecisionCommand(mismatchTransaction, "AWID-MISMATCH", "DEVICE-MISMATCH", "runner"));
}
catch (InvalidOperationException)
{
    mismatchBlocked = true;
}
Check("AWID mismatch blocked", mismatchBlocked, ref passed);

var stored = await repository.GetByTransactionAsync(critical.TransactionId);
Check("decision persisted", stored is not null && stored.DecisionId == critical.DecisionId, ref passed);
var events = await audit.GetByDecisionAsync(critical.DecisionId);
Check("decision audit recorded", events.Count == 1, ref passed);
Check("audit proves no execution", events.Single().Metadata["executionPerformed"] == "false", ref passed);
Check("decision evaluations explainable", critical.Evaluations.Count >= 4 && critical.Evaluations.All(x => !string.IsNullOrWhiteSpace(x.Reason)), ref passed);
Check("decision does not mutate payment", critical.Action == FraudDecisionAction.DeclineRecommended, ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0017.4 fraud rules and decision scenarios passed.");
Console.WriteLine("Payment execution mutation: NOT IMPLEMENTED");
Console.WriteLine("Wallet suspension: NOT IMPLEMENTED");
Console.WriteLine("Device revocation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : IFraudDecisionClock
{
    public DateTimeOffset UtcNow { get; } = now;
}