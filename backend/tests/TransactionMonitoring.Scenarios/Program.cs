using AfriWallet.Compliance.TransactionMonitoring.Application.Monitoring;
using AfriWallet.Compliance.TransactionMonitoring.Application.Rules;
using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;
using AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

static void Check(string name, bool condition)
{
    Console.WriteLine($"{name,-45} {(condition ? "PASS" : "FAIL")}");
    if (!condition)
        throw new InvalidOperationException($"Scenario failed: {name}");
}

var history = new InMemoryTransactionHistoryRepository();
var alerts = new InMemoryMonitoringAlertRepository();
var audit = new InMemoryMonitoringAuditStore();
var clock = new SystemMonitoringClock();
var service = new TransactionMonitoringService(
    history,
    alerts,
    new SandboxMonitoringRuleProvider(),
    audit,
    clock,
    new LargeAmountRuleEvaluator(),
    new VelocityRuleEvaluator(),
    new StructuringRuleEvaluator(),
    new GeographicRiskRuleEvaluator(),
    new RepeatedBeneficiaryRuleEvaluator());
var now = DateTimeOffset.UtcNow;

var normal = await service.MonitorAsync(
    new MonitorTransactionCommand(
        new MonitoredTransaction(
            Guid.NewGuid(),
            "AWID-AML-001",
            TransactionDirection.Outbound,
            TransactionChannel.Wallet,
            10_000,
            "eur",
            "fr",
            "CP-NORMAL",
            "BEN-NORMAL",
            now),
        "scenario-runner"));

Check("normal transaction monitored", normal.TransactionId != Guid.Empty);
Check("normal transaction low risk", normal.Risk.Score == 0 && normal.Risk.Band == "LOW");
Check("normal transaction no alert", normal.Alert is null);

var large = await service.MonitorAsync(
    new MonitorTransactionCommand(
        new MonitoredTransaction(
            Guid.NewGuid(),
            "AWID-AML-002",
            TransactionDirection.Outbound,
            TransactionChannel.Bank,
            1_500_000,
            "EUR",
            "FR",
            "CP-LARGE",
            "BEN-LARGE",
            now),
        "scenario-runner"));

Check("large amount rule triggered", large.Evaluations.Any(evaluation =>
    evaluation.RuleCode == "AML-LARGE-AMOUNT" && evaluation.Triggered));
Check("large amount alert generated", large.Alert is not null);

for (var index = 0; index < 2; index++)
{
    await service.MonitorAsync(
        new MonitorTransactionCommand(
            new MonitoredTransaction(
                Guid.NewGuid(),
                "AWID-AML-003",
                TransactionDirection.Outbound,
                TransactionChannel.Bank,
                800_000,
                "EUR",
                "FR",
                $"CP-STRUCT-{index}",
                $"BEN-STRUCT-{index}",
                now.AddMinutes(index)),
            "scenario-runner"));
}

var structuring = await service.MonitorAsync(
    new MonitorTransactionCommand(
        new MonitoredTransaction(
            Guid.NewGuid(),
            "AWID-AML-003",
            TransactionDirection.Outbound,
            TransactionChannel.Bank,
            800_000,
            "EUR",
            "FR",
            "CP-STRUCT-2",
            "BEN-STRUCT-2",
            now.AddMinutes(2)),
        "scenario-runner"));

Check("structuring rule triggered", structuring.Evaluations.Any(evaluation =>
    evaluation.RuleCode == "AML-STRUCTURING" && evaluation.Triggered));
Check("structuring risk elevated", structuring.Risk.Score >= 55);
Check("structuring alert generated", structuring.Alert is not null);

var geographic = await service.MonitorAsync(
    new MonitorTransactionCommand(
        new MonitoredTransaction(
            Guid.NewGuid(),
            "AWID-AML-004",
            TransactionDirection.Outbound,
            TransactionChannel.MobileMoney,
            100_000,
            "EUR",
            "XZ",
            "CP-GEO",
            "BEN-GEO",
            now),
        "scenario-runner"));

Check("sandbox geographic rule triggered", geographic.Evaluations.Any(evaluation =>
    evaluation.RuleCode == "AML-GEO-RISK" && evaluation.Triggered));

MonitoringResult? velocity = null;
for (var index = 0; index < 6; index++)
{
    velocity = await service.MonitorAsync(
        new MonitorTransactionCommand(
            new MonitoredTransaction(
                Guid.NewGuid(),
                "AWID-AML-005",
                TransactionDirection.Inbound,
                TransactionChannel.Wallet,
                20_000,
                "EUR",
                "FR",
                $"CP-VELOCITY-{index}",
                $"BEN-VELOCITY-{index}",
                now.AddSeconds(index)),
            "scenario-runner"));
}

Check("velocity rule triggered", velocity!.Evaluations.Any(evaluation =>
    evaluation.RuleCode == "AML-HIGH-VELOCITY" && evaluation.Triggered));

MonitoringResult? repeatedBeneficiary = null;
for (var index = 0; index < 5; index++)
{
    repeatedBeneficiary = await service.MonitorAsync(
        new MonitorTransactionCommand(
            new MonitoredTransaction(
                Guid.NewGuid(),
                "AWID-AML-006",
                TransactionDirection.Outbound,
                TransactionChannel.MobileMoney,
                25_000,
                "EUR",
                "FR",
                $"CP-REPEATED-{index}",
                "BEN-REPEATED",
                now.AddMinutes(index)),
            "scenario-runner"));
}

Check("repeated beneficiary rule triggered", repeatedBeneficiary!.Evaluations.Any(evaluation =>
    evaluation.RuleCode == "AML-REPEATED-BENEFICIARY" && evaluation.Triggered));

var events = await audit.GetByTransactionAsync(large.TransactionId);
Check("audit event recorded", events.Count == 1);

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0016.4 AML transaction monitoring scenarios passed.");
Console.WriteLine("AML rules: SANDBOX POLICY");
Console.WriteLine("Regulatory filing: NOT IMPLEMENTED");
Console.WriteLine("Regulatory certification: NOT CLAIMED");
Console.WriteLine("Decision: READY FOR REVIEW");