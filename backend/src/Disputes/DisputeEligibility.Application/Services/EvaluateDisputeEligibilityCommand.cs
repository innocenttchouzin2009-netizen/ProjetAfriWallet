namespace AfriWallet.Disputes.Eligibility.Application.Services;

public sealed record EvaluateDisputeEligibilityCommand(
    Guid ClaimId,
    string Actor);
