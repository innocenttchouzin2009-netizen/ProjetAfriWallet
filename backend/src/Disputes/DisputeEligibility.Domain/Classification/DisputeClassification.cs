namespace AfriWallet.Disputes.Eligibility.Domain.Classification;

public sealed record DisputeClassification(
    DisputeCategory Category,
    string Reason);
