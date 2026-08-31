using AfriWallet.Merchants.Intelligence.Application.Abstractions;
using AfriWallet.Merchants.Intelligence.Application.Models;
using AfriWallet.Merchants.Intelligence.Application.Policies;
using AfriWallet.Merchants.Intelligence.Domain.Findings;
using AfriWallet.Merchants.Intelligence.Domain.Metrics;

namespace AfriWallet.Merchants.Intelligence.Application.Services;

public sealed class MerchantIntelligenceService(IMerchantIntelligenceSource source, IMerchantIntelligenceRepository repository, IMerchantIntelligenceAuditStore audit, IMerchantIntelligenceClock clock, MerchantRiskPolicy policy)
{
    public async Task<MerchantRiskResult> EvaluateAsync(EvaluateMerchantRiskCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.MerchantId)) throw new ArgumentException("Merchant id is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor)) throw new ArgumentException("Actor is required.", nameof(command));
        var snapshot = await source.GetAsync(command.MerchantId, cancellationToken) ?? throw new KeyNotFoundException("Merchant intelligence subject not found.");
        var metrics = BuildMetrics(snapshot);
        var patterns = new List<MerchantRiskPattern>();
        DetectDeclineRate(metrics, snapshot, patterns); DetectStepUpRate(metrics, snapshot, patterns); DetectCriticalRisk(snapshot, patterns); DetectSettlementFailures(metrics, snapshot, patterns); DetectSettlementRetries(metrics, snapshot, patterns); DetectDisputeConcentration(metrics, snapshot, patterns); DetectRefundChargebackConcentration(metrics, snapshot, patterns); DetectHighCheckoutVolume(metrics, snapshot, patterns); DetectCompoundRisk(patterns);
        var score = Math.Clamp(patterns.Sum(x => x.Score), 0, 100);
        var settlementRisk = patterns.Any(x => x.Code is "MER-INT-SETTLEMENT-FAILURES" or "MER-INT-SETTLEMENT-RETRIES");
        var customerRisk = patterns.Any(x => x.Code is "MER-INT-DISPUTE-CONCENTRATION" or "MER-INT-REFUND-CHARGEBACK-CONCENTRATION");
        var finding = new MerchantRiskFinding(Guid.NewGuid(), snapshot.MerchantId, score, policy.ResolveSeverity(score), policy.ResolveRecommendation(score, settlementRisk, customerRisk), metrics, patterns, clock.UtcNow);
        await repository.SaveAsync(finding, cancellationToken);
        await audit.AppendAsync(new MerchantIntelligenceAuditEvent(Guid.NewGuid(), finding.FindingId, finding.MerchantId, "merchant.risk.evaluated", command.Actor, clock.UtcNow, new Dictionary<string,string> { ["score"] = finding.Score.ToString(), ["severity"] = finding.Severity.ToString(), ["recommendation"] = finding.Recommendation.ToString(), ["patternCount"] = finding.Patterns.Count.ToString(), ["automaticMerchantBlocking"] = "false", ["automaticMerchantSuspension"] = "false", ["automaticSettlementFreeze"] = "false", ["automaticPayoutFreeze"] = "false", ["paymentCapturePerformed"] = "false", ["moneyMovementPerformed"] = "false", ["ledgerMutationPerformed"] = "false" }), cancellationToken);
        return new(finding.FindingId, finding.MerchantId, finding.Score, finding.Severity, finding.Recommendation, finding.Metrics, finding.Patterns, finding.CreatedAtUtc);
    }

    private static MerchantCommerceMetrics BuildMetrics(MerchantIntelligenceSnapshot s)
    {
        var decisions = s.Decisions.Count; var settlements = s.Settlements.Count;
        var auth = s.Decisions.Count(x => x.DecisionType is "Authorize" or "CaptureEligible"); var declines = s.Decisions.Count(x => x.DecisionType == "Decline"); var stepUps = s.Decisions.Count(x => x.DecisionType == "RequiresStepUp");
        var failures = s.Settlements.Count(x => x.Status is "Failed" or "ManualInterventionRequired"); var retries = s.Settlements.Sum(x => Math.Max(0, x.AttemptCount - 1));
        var refunds = s.Disputes.Count(x => x.DecisionType == "RefundRecommended"); var chargebacks = s.Disputes.Count(x => x.DecisionType == "ChargebackRecommended");
        return new(s.Checkouts.Count, s.Checkouts.Count(x => x.Status == "ReadyForPayment"), auth, declines, stepUps, s.Decisions.Count(x => x.DecisionType == "CaptureEligible"), settlements, failures, retries, s.Disputes.Count, refunds, chargebacks, decisions == 0 ? 0 : (double)declines / decisions, decisions == 0 ? 0 : (double)stepUps / decisions, settlements == 0 ? 0 : (double)failures / settlements);
    }
    private static void DetectDeclineRate(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { if (s.Decisions.Count < 3 || m.DeclineRate < .4) return; p.Add(new("MER-INT-HIGH-DECLINE-RATE", m.DeclineRate >= .7 ? 30 : 20, $"Decline rate is {m.DeclineRate:P0} across {s.Decisions.Count} decisions.", s.Decisions.Where(x => x.DecisionType == "Decline").Select(x => x.PaymentIntentId.ToString("D")).ToArray())); }
    private static void DetectStepUpRate(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { if (s.Decisions.Count < 3 || m.StepUpRate < .3) return; p.Add(new("MER-INT-HIGH-STEP-UP-RATE", 15, $"Step-up rate is {m.StepUpRate:P0}.", s.Decisions.Where(x => x.DecisionType == "RequiresStepUp").Select(x => x.PaymentIntentId.ToString("D")).ToArray())); }
    private static void DetectCriticalRisk(MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { var hits = s.Decisions.Where(x => x.RiskScore >= 85).ToArray(); if (hits.Length > 0) p.Add(new("MER-INT-CRITICAL-RISK-SIGNALS", Math.Min(30, 20 + hits.Length * 5), $"{hits.Length} critical risk-score snapshots observed.", hits.Select(x => x.PaymentIntentId.ToString("D")).ToArray())); }
    private static void DetectSettlementFailures(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { if (m.SettlementFailureCount == 0) return; p.Add(new("MER-INT-SETTLEMENT-FAILURES", Math.Min(30, 15 + m.SettlementFailureCount * 5), $"{m.SettlementFailureCount} settlement workflows failed or require intervention.", s.Settlements.Where(x => x.Status is "Failed" or "ManualInterventionRequired").Select(x => x.SettlementId.ToString("D")).ToArray())); }
    private static void DetectSettlementRetries(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { if (m.SettlementRetryCount < 2) return; p.Add(new("MER-INT-SETTLEMENT-RETRIES", 15, $"{m.SettlementRetryCount} additional settlement attempts observed.", s.Settlements.Where(x => x.AttemptCount > 1).Select(x => x.SettlementId.ToString("D")).ToArray())); }
    private static void DetectDisputeConcentration(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { if (m.DisputeCount < 3) return; p.Add(new("MER-INT-DISPUTE-CONCENTRATION", Math.Min(25, 10 + m.DisputeCount * 3), $"{m.DisputeCount} disputes are associated with this merchant.", s.Disputes.Select(x => x.ClaimId.ToString("D")).ToArray())); }
    private static void DetectRefundChargebackConcentration(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { var total = m.RefundRecommendationCount + m.ChargebackRecommendationCount; if (total < 2) return; p.Add(new("MER-INT-REFUND-CHARGEBACK-CONCENTRATION", Math.Min(25, 10 + total * 4), $"{total} disputes resulted in refund or chargeback recommendations.", s.Disputes.Where(x => x.DecisionType is "RefundRecommended" or "ChargebackRecommended").Select(x => x.ClaimId.ToString("D")).ToArray())); }
    private static void DetectHighCheckoutVolume(MerchantCommerceMetrics m, MerchantIntelligenceSnapshot s, ICollection<MerchantRiskPattern> p) { if (m.CheckoutCount < 100) return; p.Add(new("MER-INT-HIGH-CHECKOUT-VOLUME", 5, $"{m.CheckoutCount} checkout sessions observed.", s.Checkouts.Take(20).Select(x => x.CheckoutSessionId.ToString("D")).ToArray())); }
    private static void DetectCompoundRisk(ICollection<MerchantRiskPattern> p) { if (p.Count < 3) return; p.Add(new("MER-INT-COMPOUND-RISK", 20, "Three or more independent merchant-risk patterns are present.", p.SelectMany(x => x.References).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())); }
}
