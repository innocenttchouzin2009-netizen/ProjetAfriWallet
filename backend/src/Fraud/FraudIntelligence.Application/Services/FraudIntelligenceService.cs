using AfriWallet.Fraud.Intelligence.Application.Abstractions;
using AfriWallet.Fraud.Intelligence.Application.Models;
using AfriWallet.Fraud.Intelligence.Application.Policies;
using AfriWallet.Fraud.Intelligence.Domain.Findings;
using AfriWallet.Fraud.Intelligence.Domain.Patterns;

namespace AfriWallet.Fraud.Intelligence.Application.Services;

public sealed class FraudIntelligenceService(
    IFraudIntelligenceSource source,
    IFraudIntelligenceRepository repository,
    IFraudIntelligenceAuditStore audit,
    IFraudIntelligenceClock clock,
    FraudCorrelationPolicy policy)
{
    public async Task<FraudCorrelationResult> CorrelateAsync(CorrelateFraudCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Awid)) throw new ArgumentException("AWID is required.", nameof(command));
        var subject = await source.GetAsync(command.Awid.Trim(), cancellationToken) ?? throw new InvalidOperationException("Intelligence source snapshot was not found.");
        if (!string.Equals(subject.Awid, command.Awid.Trim(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Source AWID does not match the command AWID.");

        var network = await source.GetNetworkAsync(cancellationToken);
        var now = clock.UtcNow;
        var patterns = new List<FraudPattern>();
        DetectSharedDevice(subject, network, patterns, now);
        DetectSharedBeneficiary(subject, network, patterns, now);
        DetectHighRiskTransactions(subject, patterns, now);
        DetectRepeatedCases(subject, patterns, now);
        if (patterns.Count >= 3)
        {
            patterns.Add(new FraudPattern(Guid.NewGuid(), FraudPatternType.CompoundRisk, Math.Min(25, 10 + patterns.Count * 5), $"{patterns.Count} independent intelligence patterns converge on the subject.", patterns.SelectMany(x => x.EntityIds).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), now));
        }

        var score = Math.Min(100, patterns.Sum(x => x.Score));
        var finding = new IntelligenceFinding(Guid.NewGuid(), subject.Awid, score, policy.ResolveSeverity(score), patterns.ToArray(), now);
        await repository.SaveAsync(finding, cancellationToken);
        await audit.AppendAsync(new FraudIntelligenceAuditEvent(Guid.NewGuid(), finding.FindingId, finding.SubjectAwid, "intelligence.correlated", command.Actor, now, new Dictionary<string, string>
        {
            ["score"] = finding.CorrelationScore.ToString(), ["severity"] = finding.Severity.ToString(), ["patternCount"] = finding.Patterns.Count.ToString(), ["enforcementPerformed"] = "false", ["machineLearning"] = "false"
        }), cancellationToken);
        return new FraudCorrelationResult(finding.FindingId, finding.SubjectAwid, finding.CorrelationScore, finding.Severity, finding.Patterns, finding.CreatedAtUtc);
    }

    private static void DetectSharedDevice(IntelligenceSourceSnapshot subject, IReadOnlyCollection<IntelligenceSourceSnapshot> network, ICollection<FraudPattern> patterns, DateTimeOffset now)
    {
        var subjectDevices = subject.Transactions.Select(x => x.DeviceId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var related = network.Where(x => !string.Equals(x.Awid, subject.Awid, StringComparison.OrdinalIgnoreCase) && x.Transactions.Any(t => subjectDevices.Contains(t.DeviceId))).Select(x => x.Awid).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (related.Length == 0) return;
        patterns.Add(new FraudPattern(Guid.NewGuid(), FraudPatternType.SharedDevice, Math.Min(30, 15 + related.Length * 5), $"Device reuse detected across {related.Length + 1} AWIDs.", new[] { subject.Awid }.Concat(related).ToArray(), now));
    }

    private static void DetectSharedBeneficiary(IntelligenceSourceSnapshot subject, IReadOnlyCollection<IntelligenceSourceSnapshot> network, ICollection<FraudPattern> patterns, DateTimeOffset now)
    {
        var beneficiaries = subject.Transactions.Select(x => x.BeneficiaryId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var related = network.Where(x => !string.Equals(x.Awid, subject.Awid, StringComparison.OrdinalIgnoreCase) && x.Transactions.Any(t => beneficiaries.Contains(t.BeneficiaryId))).Select(x => x.Awid).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (related.Length == 0) return;
        patterns.Add(new FraudPattern(Guid.NewGuid(), FraudPatternType.SharedBeneficiary, Math.Min(25, 10 + related.Length * 5), $"Beneficiary convergence detected across {related.Length + 1} AWIDs.", new[] { subject.Awid }.Concat(related).ToArray(), now));
    }

    private static void DetectHighRiskTransactions(IntelligenceSourceSnapshot subject, ICollection<FraudPattern> patterns, DateTimeOffset now)
    {
        var highRisk = subject.Transactions.Where(x => x.FraudScore >= 60).ToArray();
        if (highRisk.Length < 2) return;
        patterns.Add(new FraudPattern(Guid.NewGuid(), FraudPatternType.RepeatedHighRiskTransactions, Math.Min(35, 15 + highRisk.Length * 5), $"{highRisk.Length} transactions have fraud scores >= 60.", highRisk.Select(x => x.TransactionId.ToString("D")).ToArray(), now));
    }

    private static void DetectRepeatedCases(IntelligenceSourceSnapshot subject, ICollection<FraudPattern> patterns, DateTimeOffset now)
    {
        var cases = subject.Cases.ToArray();
        if (cases.Length < 2) return;
        patterns.Add(new FraudPattern(Guid.NewGuid(), FraudPatternType.RepeatedFraudCases, Math.Min(25, 10 + cases.Length * 5), $"{cases.Length} fraud investigation cases are linked to the AWID.", cases.Select(x => x.CaseId.ToString("D")).ToArray(), now));
    }
}