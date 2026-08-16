using AfriWallet.Compliance.RiskScoring.Application.Policies;
using AfriWallet.Compliance.RiskScoring.Application.Scoring;
using AfriWallet.Compliance.RiskScoring.Domain.Profiles;
using AfriWallet.Compliance.RiskScoring.Infrastructure;
using AfriWallet.Compliance.RiskScoring.Infrastructure.Gateways;

static void Check(string name, bool condition)
{
    Console.WriteLine($"{name,-45} {(condition ? "PASS" : "FAIL")}");
    if (!condition)
        throw new InvalidOperationException($"Scenario failed: {name}");
}

var repository = new InMemoryRiskProfileRepository();
var audit = new InMemoryRiskAuditStore();
var service = new FinancialRiskScoringService(
    new SandboxKycRiskSignalProvider(),
    new SandboxScreeningRiskSignalProvider(),
    new SandboxAmlRiskSignalProvider(),
    repository,
    audit,
    new SystemRiskClock(),
    new RiskScoringPolicy());

var low = await service.CalculateAsync(
    new CalculateRiskCommand("AWID-RISK-LOW", "scenario-runner"));
Check("low risk profile created", low.RiskProfileId != Guid.Empty);
Check("low risk score", low.Score == 6);
Check("low risk band", low.Band == RiskBand.Low);
Check("low risk allowed", low.Decision == RiskDecision.Allow);
Check("risk contributions explainable", low.Contributions.Count == 3 && low.Contributions.All(item =>
    item.Weight > 0 &&
    item.WeightedScore == item.RawScore * item.Weight &&
    !string.IsNullOrWhiteSpace(item.Reason)));

var pep = await service.CalculateAsync(
    new CalculateRiskCommand("AWID-RISK-PEP", "scenario-runner"));
Check("PEP increases risk", pep.Score > low.Score);
Check("PEP contribution present", pep.Contributions.Any(item =>
    item.FactorCode == "RISK-SCREENING" && item.RawScore == 65));

var amlHigh = await service.CalculateAsync(
    new CalculateRiskCommand("AWID-RISK-AML-HIGH", "scenario-runner"));
Check("AML high contribution present", amlHigh.Contributions.Any(item =>
    item.FactorCode == "RISK-AML" && item.RawScore == 85));
Check("AML risk elevated", amlHigh.Score > low.Score);

var blocked = await service.CalculateAsync(
    new CalculateRiskCommand("AWID-RISK-BLOCK", "scenario-runner"));
Check("sanctions block restricts", blocked.Decision == RiskDecision.Restrict);
Check("sanctions contribution critical", blocked.Contributions.Any(item =>
    item.FactorCode == "RISK-SCREENING" && item.RawScore == 100));

var unverified = await service.CalculateAsync(
    new CalculateRiskCommand("AWID-UNVERIFIED", "scenario-runner"));
Check("unverified KYC increases risk", unverified.Contributions.Any(item =>
    item.FactorCode == "RISK-KYC" && item.RawScore >= 70));

var stored = await repository.GetLatestAsync(blocked.Awid);
Check("latest risk profile persisted", stored is not null && stored.ProfileId == blocked.RiskProfileId);

var events = await audit.GetByAwidAsync(blocked.Awid);
Check("risk audit recorded", events.Count == 1);

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0016.5 financial risk scoring scenarios passed.");
Console.WriteLine("Risk policy: SANDBOX");
Console.WriteLine("Source engines duplicated: NO");
Console.WriteLine("Regulatory/legal decision: NOT CLAIMED");
Console.WriteLine("Decision: READY FOR REVIEW");