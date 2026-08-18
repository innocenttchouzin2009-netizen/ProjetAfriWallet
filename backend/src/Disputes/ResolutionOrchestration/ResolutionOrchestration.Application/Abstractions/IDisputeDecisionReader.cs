namespace AfriWallet.Disputes.Resolution.Application.Abstractions;

public sealed record DisputeDecisionSnapshot(
    Guid DecisionId,
    Guid ClaimId,
    Guid InvestigationId,
    string Awid,
    string DecisionType,
    string Status,
    string PolicyVersion,
    DateTimeOffset UpdatedAtUtc);

public interface IDisputeDecisionReader
{
    Task<DisputeDecisionSnapshot?> GetAsync(Guid decisionId, CancellationToken cancellationToken = default);
}
