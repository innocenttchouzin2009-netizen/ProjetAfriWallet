using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;
using AfriWallet.Fraud.TransactionFraud.Application.Policies;
using AfriWallet.Fraud.TransactionFraud.Application.Services;
using AfriWallet.Fraud.TransactionFraud.Domain.Detection;
using AfriWallet.Fraud.TransactionFraud.Domain.Factors;
using AfriWallet.Fraud.TransactionFraud.Domain.Signals;
using AfriWallet.Fraud.TransactionFraud.Domain.Transactions;
using AfriWallet.Fraud.TransactionFraud.Infrastructure;

static void Check(string name, bool ok, ref int passed)
{
    Console.WriteLine($"{name,-46} {(ok ? "PASS" : "FAIL")}");
    if (!ok)
        throw new InvalidOperationException(name);
    passed++;
}

var passed = 0;
var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

var fraudSignals = new SandboxFraudSignalReader();
var deviceRiskReader = new SandboxDeviceRiskReader();
var repository = new InMemoryTransactionFraudRepository();
var audit = new InMemoryTransactionFraudAuditStore();
var service = new TransactionFraudDetectionService(
    fraudSignals,
    deviceRiskReader,
    repository,
    audit,
    new FixedClock(now),
    new TransactionFraudPolicy());

var baselineTx = new FraudTransaction(
    Guid.NewGuid(),
    "AWID-BASE",
    "DEVICE-BASE",
    "BEN-BASE",
    100,
    "USD",
    "US",
    now);
var baseline = await service.DetectAsync(new DetectTransactionFraudCommand(baselineTx, "runner"));
Check("baseline detection created", baseline.DetectionId != Guid.Empty, ref passed);
Check("baseline score low", baseline.Score < 30, ref passed);
Check("baseline recommendation allow", baseline.Recommendation == FraudDetectionRecommendation.Allow, ref passed);

var unusual = new FraudTransaction(Guid.NewGuid(), "AWID-UNUSUAL", "DEVICE-UNUSUAL", "BEN-UNUSUAL", 12_500_000, "USD", "US", now);
var unusualResult = await service.DetectAsync(new DetectTransactionFraudCommand(unusual, "runner"));
Check("unusual amount factor detected", unusualResult.Factors.Any(x => x.Type == TransactionFraudFactorType.UnusualAmount), ref passed);

var beneficiaryTx = new FraudTransaction(Guid.NewGuid(), "AWID-BEN", "DEVICE-BEN", "BEN-NEW", 1_200, "USD", "US", now);
fraudSignals.Add(new FraudSignalSnapshot("benef-001", "BeneficiaryAdded", "BENEFICIARY", "BEN-NEW", "Medium", now.AddHours(-2), new Dictionary<string, string> { ["source"] = "sandbox" }));
var beneficiaryResult = await service.DetectAsync(new DetectTransactionFraudCommand(beneficiaryTx, "runner"));
Check("new beneficiary factor detected", beneficiaryResult.Factors.Any(x => x.Type == TransactionFraudFactorType.NewBeneficiary), ref passed);

var deviceTx = new FraudTransaction(Guid.NewGuid(), "AWID-DEVICE", "DEVICE-CHANGED", "BEN-DEVICE", 1_500, "USD", "US", now);
fraudSignals.Add(new FraudSignalSnapshot("device-change-001", "DeviceChanged", "DEVICE", "DEVICE-CHANGED", "High", now.AddHours(-1), new Dictionary<string, string> { ["source"] = "sandbox" }));
var deviceResult = await service.DetectAsync(new DetectTransactionFraudCommand(deviceTx, "runner"));
Check("recent device change detected", deviceResult.Factors.Any(x => x.Type == TransactionFraudFactorType.RecentDeviceChange), ref passed);

deviceRiskReader.Set(new DeviceRiskSnapshot("AWID-DEVICE-RISK", "DEVICE-RISK", 80, "Critical", "DeclineRecommended", now));
var deviceRiskTx = new FraudTransaction(Guid.NewGuid(), "AWID-DEVICE-RISK", "DEVICE-RISK", "BEN-RISK", 2_000, "USD", "US", now);
var riskResult = await service.DetectAsync(new DetectTransactionFraudCommand(deviceRiskTx, "runner"));
Check("device risk factor detected", riskResult.Factors.Any(x => x.Type == TransactionFraudFactorType.DeviceRisk), ref passed);
Check("device risk increases score", riskResult.Score >= 30, ref passed);

for (var i = 0; i < 5; i++)
    fraudSignals.Add(new FraudSignalSnapshot($"attempt-{i}", "PaymentAttempted", "AWID", "AWID-VELOCITY", "Low", now.AddMinutes(i), new Dictionary<string, string>()));
var velocityTx = new FraudTransaction(Guid.NewGuid(), "AWID-VELOCITY", "DEVICE-VELOCITY", "BEN-VELOCITY", 5_000, "USD", "US", now);
var velocityResult = await service.DetectAsync(new DetectTransactionFraudCommand(velocityTx, "runner"));
Check("transaction velocity detected", velocityResult.Factors.Any(x => x.Type == TransactionFraudFactorType.HighTransactionVelocity), ref passed);

var geoTx = new FraudTransaction(Guid.NewGuid(), "AWID-GEO", "DEVICE-GEO", "BEN-GEO", 4_200, "USD", "FR", now);
fraudSignals.Add(new FraudSignalSnapshot("country-001", "PaymentAttempted", "AWID", "AWID-GEO", "Low", now.AddHours(-6), new Dictionary<string, string> { ["countryCode"] = "US" }));
var geoResult = await service.DetectAsync(new DetectTransactionFraudCommand(geoTx, "runner"));
Check("geographic anomaly detected", geoResult.Factors.Any(x => x.Type == TransactionFraudFactorType.GeographicAnomaly), ref passed);

for (var i = 0; i < 4; i++)
    fraudSignals.Add(new FraudSignalSnapshot($"repeat-{i}", i % 2 == 0 ? "PaymentAttempted" : "PaymentFailed", "AWID", "AWID-REPEAT", "Low", now.AddMinutes(i), new Dictionary<string, string>()));
var repeatTx = new FraudTransaction(Guid.NewGuid(), "AWID-REPEAT", "DEVICE-REPEAT", "BEN-REPEAT", 1_800, "USD", "US", now);
var repeatResult = await service.DetectAsync(new DetectTransactionFraudCommand(repeatTx, "runner"));
Check("repeated attempts factor detected", repeatResult.Factors.Any(x => x.Type == TransactionFraudFactorType.RepeatedAttempts), ref passed);

fraudSignals.Add(new FraudSignalSnapshot("failed-001", "PaymentFailed", "AWID", "AWID-FAIL-THEN", "Medium", now.AddMinutes(-30), new Dictionary<string, string>()));
fraudSignals.Add(new FraudSignalSnapshot("success-001", "PaymentAttempted", "AWID", "AWID-FAIL-THEN", "Low", now.AddMinutes(-10), new Dictionary<string, string>()));
var failThenTx = new FraudTransaction(Guid.NewGuid(), "AWID-FAIL-THEN", "DEVICE-FAIL-THEN", "BEN-FAIL-THEN", 1_600, "USD", "US", now);
var failThenResult = await service.DetectAsync(new DetectTransactionFraudCommand(failThenTx, "runner"));
Check("failed then success factor detected", failThenResult.Factors.Any(x => x.Type == TransactionFraudFactorType.FailedThenSuccessfulPayment), ref passed);

var compound = new FraudTransaction(Guid.NewGuid(), "AWID-COMPOUND", "DEVICE-COMPOUND", "BEN-COMPOUND", 25_000_000, "USD", "FR", now);
fraudSignals.Add(new FraudSignalSnapshot("compound-new-device", "NewDevice", "DEVICE", "DEVICE-COMPOUND", "High", now.AddHours(-1), new Dictionary<string, string>()));
fraudSignals.Add(new FraudSignalSnapshot("compound-benef", "BeneficiaryAdded", "BENEFICIARY", "BEN-COMPOUND", "Medium", now.AddHours(-3), new Dictionary<string, string>()));
fraudSignals.Add(new FraudSignalSnapshot("compound-fail", "PaymentFailed", "AWID", "AWID-COMPOUND", "Medium", now.AddMinutes(-20), new Dictionary<string, string>()));
fraudSignals.Add(new FraudSignalSnapshot("compound-attempt", "PaymentAttempted", "AWID", "AWID-COMPOUND", "Low", now.AddMinutes(-10), new Dictionary<string, string>()));
deviceRiskReader.Set(new DeviceRiskSnapshot("AWID-COMPOUND", "DEVICE-COMPOUND", 70, "High", "Challenge", now));
var compoundResult = await service.DetectAsync(new DetectTransactionFraudCommand(compound, "runner"));
Check("compound risk high or critical", compoundResult.Band is FraudDetectionBand.High or FraudDetectionBand.Critical, ref passed);
Check("compound recommendation not allow", compoundResult.Recommendation != FraudDetectionRecommendation.Allow, ref passed);

var stored = await repository.GetByTransactionAsync(compoundResult.TransactionId);
Check("fraud detection persisted", stored is not null && stored.DetectionId == compoundResult.DetectionId, ref passed);
var auditEvents = await audit.GetByDetectionAsync(compoundResult.DetectionId);
Check("fraud audit recorded", auditEvents.Count == 1, ref passed);
Check("recommendation remains non-executing", compoundResult.Recommendation is FraudDetectionRecommendation.Allow or FraudDetectionRecommendation.Review or FraudDetectionRecommendation.Challenge or FraudDetectionRecommendation.DeclineRecommended, ref passed);

Console.WriteLine();
Console.WriteLine($"Checks: {passed}");
Console.WriteLine($"Passed: {passed}");
Console.WriteLine("Failed: 0");
Console.WriteLine("Skipped: 0");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0017.3 transaction fraud detection scenarios passed.");
Console.WriteLine("Automatic payment decline: NOT IMPLEMENTED");
Console.WriteLine("Payment mutation: NOT IMPLEMENTED");
Console.WriteLine("Decision: READY FOR REVIEW");

sealed class FixedClock(DateTimeOffset now) : ITransactionFraudClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
