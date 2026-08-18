using AfriWallet.Disputes.Eligibility.Application.Abstractions;
using AfriWallet.Disputes.Eligibility.Domain.Claims;
using AfriWallet.Disputes.Eligibility.Domain.Eligibility;

namespace AfriWallet.Disputes.Eligibility.Application.Policies;

public sealed class DisputeEligibilityPolicy
{
    /// Sandbox submission window; not a universal regulatory requirement.
    public const int DefaultWindowDays = 120;

    public IReadOnlyCollection<EligibilityRuleEvaluation> Evaluate(
        DisputeClaimSnapshot claim,
        TransactionReferenceSnapshot transaction)
    {
        var elapsed = claim.SubmittedAtUtc - transaction.OccurredAtUtc;

        return
        [
            Rule("DSP-ELG-001",
                string.Equals(claim.Awid, transaction.Awid, StringComparison.OrdinalIgnoreCase),
                "Claim AWID must match transaction AWID."),
            Rule("DSP-ELG-002",
                string.Equals(claim.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase),
                "Claim currency must match transaction currency."),
            Rule("DSP-ELG-003",
                claim.ClaimAmountMinor > 0 && claim.ClaimAmountMinor <= transaction.AmountMinor,
                "Claim amount must be positive and cannot exceed transaction amount."),
            Rule("DSP-ELG-004",
                elapsed >= TimeSpan.Zero && elapsed <= TimeSpan.FromDays(DefaultWindowDays),
                $"Claim must be submitted within {DefaultWindowDays} days."),
            Rule("DSP-ELG-005",
                IsSettled(transaction.Status),
                "Transaction must be completed or settled."),
            Rule("DSP-ELG-006",
                claim.ClaimType != DisputeClaimType.Other,
                "Specific claim types are automatically classifiable.")
        ];
    }

    public static bool IsSettled(string status) =>
        string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Settled", StringComparison.OrdinalIgnoreCase);

    private static EligibilityRuleEvaluation Rule(string code, bool passed, string reason) => new(code, passed, reason);
}
