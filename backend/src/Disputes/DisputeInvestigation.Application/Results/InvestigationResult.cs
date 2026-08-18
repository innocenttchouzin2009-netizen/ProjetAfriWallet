using AfriWallet.Disputes.Investigation.Domain.Cases;

namespace AfriWallet.Disputes.Investigation.Application.Results;

public sealed record InvestigationResult(
    Guid InvestigationId,
    Guid ClaimId,
    string Awid,
    string? AnalystId,
    InvestigationStatus Status,
    InvestigationOutcome Outcome,
    int EvidenceCount,
    int OpenEvidenceRequests,
    int TimelineCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
