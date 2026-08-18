namespace AfriWallet.Disputes.Investigation.Application.Abstractions;

public sealed record DisputeEligibilitySnapshot(
    Guid DecisionId,
    Guid ClaimId,
    string Awid,
    string Status,
    string Category,
    DateTimeOffset EvaluatedAtUtc);

public interface IDisputeEligibilityReader
{
    Task<DisputeEligibilitySnapshot?> GetByClaimAsync(Guid claimId, CancellationToken cancellationToken = default);
}
