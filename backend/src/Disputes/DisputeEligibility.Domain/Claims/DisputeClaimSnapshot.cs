namespace AfriWallet.Disputes.Eligibility.Domain.Claims;

public sealed record DisputeClaimSnapshot(
    Guid ClaimId,
    string Awid,
    Guid TransactionId,
    DisputeClaimType ClaimType,
    long ClaimAmountMinor,
    string Currency,
    string Description,
    DateTimeOffset SubmittedAtUtc,
    DisputeChannel Channel);
