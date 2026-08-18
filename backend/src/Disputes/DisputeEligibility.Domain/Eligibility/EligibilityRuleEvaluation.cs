namespace AfriWallet.Disputes.Eligibility.Domain.Eligibility;

public sealed record EligibilityRuleEvaluation(
    string RuleCode,
    bool Passed,
    string Reason);
