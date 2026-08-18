using AfriWallet.Disputes.Intelligence.Application.Abstractions;
using AfriWallet.Disputes.Intelligence.Application.Models;
using AfriWallet.Disputes.Intelligence.Application.Policies;
using AfriWallet.Disputes.Intelligence.Domain.Findings;
using AfriWallet.Disputes.Intelligence.Domain.Metrics;

namespace AfriWallet.Disputes.Intelligence.Application.Services;

/// Produces analytical customer-protection findings only.
/// It never blocks a merchant, suspends a customer, executes a refund, or mutates the ledger.
public sealed class CustomerProtectionService(
    IDisputeIntelligenceSource source,
    IDisputeIntelligenceRepository repository,
    IDisputeIntelligenceAuditStore audit,
    IDisputeIntelligenceClock clock,
    CustomerProtectionPolicy policy)
{
    public async Task<CustomerProtectionResult> EvaluateAsync(EvaluateProtectionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.SubjectId))
            throw new ArgumentException("Subject id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));

        var snapshot = await source.GetAsync(command.SubjectId, cancellationToken)
            ?? throw new KeyNotFoundException("Dispute intelligence subject not found.");

        var metrics = BuildMetrics(snapshot);

        var patterns = new List<ProtectionPattern>();
        DetectRepeatedClaims(snapshot, patterns);
        DetectMerchantConcentration(snapshot, patterns);
        DetectBeneficiaryConcentration(snapshot, patterns);
        DetectFavorableDecisionConcentration(snapshot, metrics, patterns);
        DetectResolutionFailures(snapshot, patterns);
        DetectSlowResolution(metrics, patterns);
        DetectCompoundProtectionRisk(patterns);

        var score = Math.Clamp(patterns.Sum(x => x.Score), 0, 100);
        var severity = policy.ResolveSeverity(score);
        var merchantConcentration = patterns.Any(x => x.Code == "DSP-INT-MERCHANT-CONCENTRATION");
        var failedResolutions = metrics.FailedResolutionCount > 0;
        var recommendation = policy.ResolveRecommendation(score, merchantConcentration, failedResolutions);

        var finding = new ProtectionFinding(
            Guid.NewGuid(), snapshot.SubjectId, score, severity, recommendation, metrics, patterns, clock.UtcNow);

        await repository.SaveAsync(finding, cancellationToken);
        await AuditAsync(finding, command.Actor, cancellationToken);
        return Map(finding);
    }

    private static DisputeIntelligenceMetrics BuildMetrics(DisputeIntelligenceSnapshot snapshot)
    {
        var eligibleClaimCount = snapshot.Eligibility.Count(x => string.Equals(x.Status, "Eligible", StringComparison.OrdinalIgnoreCase));
        var refundRecommendationCount = snapshot.Decisions.Count(x => string.Equals(x.DecisionType, "RefundRecommended", StringComparison.OrdinalIgnoreCase));
        var chargebackRecommendationCount = snapshot.Decisions.Count(x => string.Equals(x.DecisionType, "ChargebackRecommended", StringComparison.OrdinalIgnoreCase));
        var favorableDecisionCount = refundRecommendationCount + chargebackRecommendationCount;
        var failedResolutionCount = snapshot.Resolutions.Count(x =>
            string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Status, "ManualInterventionRequired", StringComparison.OrdinalIgnoreCase));
        var repeatedMerchantCount = snapshot.Claims
            .GroupBy(x => x.MerchantId, StringComparer.OrdinalIgnoreCase)
            .Count(g => g.Count() >= 2);

        var completedInvestigations = snapshot.Investigations
            .Where(x => x.CompletedAtUtc.HasValue)
            .Select(x => (x.CompletedAtUtc!.Value - x.StartedAtUtc).TotalHours)
            .ToArray();
        var averageResolutionHours = completedInvestigations.Length > 0 ? completedInvestigations.Average() : 0d;

        return new DisputeIntelligenceMetrics(
            snapshot.Claims.Count,
            eligibleClaimCount,
            favorableDecisionCount,
            refundRecommendationCount,
            chargebackRecommendationCount,
            failedResolutionCount,
            repeatedMerchantCount,
            averageResolutionHours);
    }

    private static void DetectRepeatedClaims(DisputeIntelligenceSnapshot snapshot, ICollection<ProtectionPattern> patterns)
    {
        if (snapshot.Claims.Count < 3)
            return;

        patterns.Add(new ProtectionPattern(
            "DSP-INT-REPEATED-CLAIMS",
            Math.Min(25, 10 + (snapshot.Claims.Count - 3) * 5),
            $"{snapshot.Claims.Count} dispute claims submitted by this subject.",
            snapshot.Claims.Select(x => x.ClaimId.ToString("D")).ToArray()));
    }

    private static void DetectMerchantConcentration(DisputeIntelligenceSnapshot snapshot, ICollection<ProtectionPattern> patterns)
    {
        var top = snapshot.Claims
            .GroupBy(x => x.MerchantId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        if (top is null)
            return;

        patterns.Add(new ProtectionPattern(
            "DSP-INT-MERCHANT-CONCENTRATION",
            Math.Min(25, 10 + top.Count() * 5),
            $"{top.Count()} disputes linked to merchant {top.Key}.",
            top.Select(x => x.ClaimId.ToString("D")).ToArray()));
    }

    private static void DetectBeneficiaryConcentration(DisputeIntelligenceSnapshot snapshot, ICollection<ProtectionPattern> patterns)
    {
        var top = snapshot.Claims
            .GroupBy(x => x.BeneficiaryId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        if (top is null)
            return;

        patterns.Add(new ProtectionPattern(
            "DSP-INT-BENEFICIARY-CONCENTRATION",
            Math.Min(25, 10 + top.Count() * 5),
            $"{top.Count()} disputes linked to beneficiary {top.Key}.",
            top.Select(x => x.ClaimId.ToString("D")).ToArray()));
    }

    private static void DetectFavorableDecisionConcentration(
        DisputeIntelligenceSnapshot snapshot,
        DisputeIntelligenceMetrics metrics,
        ICollection<ProtectionPattern> patterns)
    {
        if (metrics.FavorableDecisionCount < 2)
            return;

        patterns.Add(
            new ProtectionPattern(
                "DSP-INT-FAVORABLE-DECISION-CONCENTRATION",
                Math.Min(20, 8 + metrics.FavorableDecisionCount * 3),
                $"{metrics.FavorableDecisionCount} dispute decisions favor refund or chargeback.",
                snapshot.Decisions
                    .Where(x => x.DecisionType is "RefundRecommended" or "ChargebackRecommended")
                    .Select(x => x.ClaimId.ToString("D"))
                    .ToArray()));
    }

    private static void DetectResolutionFailures(DisputeIntelligenceSnapshot snapshot, ICollection<ProtectionPattern> patterns)
    {
        var failures = snapshot.Resolutions
            .Where(x =>
                string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Status, "ManualInterventionRequired", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (failures.Length == 0)
            return;

        patterns.Add(
            new ProtectionPattern(
                "DSP-INT-RESOLUTION-FAILURES",
                Math.Min(30, 15 + failures.Length * 5),
                $"{failures.Length} dispute resolution workflows did not complete normally.",
                failures.Select(x => x.ClaimId.ToString("D")).ToArray()));
    }

    private static void DetectSlowResolution(DisputeIntelligenceMetrics metrics, ICollection<ProtectionPattern> patterns)
    {
        if (metrics.AverageResolutionHours <= 72)
            return;

        patterns.Add(
            new ProtectionPattern(
                "DSP-INT-SLOW-RESOLUTION",
                15,
                $"Average investigation resolution time is {metrics.AverageResolutionHours:F2} hours.",
                Array.Empty<string>()));
    }

    private static void DetectCompoundProtectionRisk(ICollection<ProtectionPattern> patterns)
    {
        if (patterns.Count < 3)
            return;

        var references = patterns
            .SelectMany(x => x.References)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        patterns.Add(
            new ProtectionPattern(
                "DSP-INT-COMPOUND-RISK",
                20,
                "Three or more independent dispute protection patterns are present.",
                references));
    }

    private async Task AuditAsync(ProtectionFinding finding, string actor, CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new DisputeIntelligenceAuditEvent(
                Guid.NewGuid(),
                finding.FindingId,
                finding.SubjectId,
                "protection.evaluated",
                actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["score"] = finding.Score.ToString(),
                    ["severity"] = finding.Severity.ToString(),
                    ["recommendation"] = finding.Recommendation.ToString(),
                    ["patternCount"] = finding.Patterns.Count.ToString(),
                    ["automaticMerchantBlockingPerformed"] = "false",
                    ["automaticCustomerSuspensionPerformed"] = "false",
                    ["refundExecutionPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false",
                    ["ledgerMutationPerformed"] = "false"
                }),
            cancellationToken);
    }

    private static CustomerProtectionResult Map(ProtectionFinding finding) => new(
        finding.FindingId,
        finding.SubjectId,
        finding.Score,
        finding.Severity,
        finding.Recommendation,
        finding.Metrics,
        finding.Patterns,
        finding.CreatedAtUtc);
}
