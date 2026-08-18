namespace AfriWallet.Disputes.Intelligence.Application.Models;

public sealed record ClaimSnapshot(
    Guid ClaimId,
    string Awid,
    string MerchantId,
    string BeneficiaryId,
    string ClaimType,
    DateTimeOffset SubmittedAtUtc);

public sealed record EligibilitySnapshot(Guid ClaimId, string Status, string Category);

public sealed record InvestigationSnapshot(
    Guid ClaimId,
    string Outcome,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record DecisionSnapshot(Guid ClaimId, string DecisionType, string Status);

public sealed record ResolutionSnapshot(Guid ClaimId, string Route, string Status, int AttemptCount);

public sealed record DisputeIntelligenceSnapshot(
    string SubjectId,
    IReadOnlyCollection<ClaimSnapshot> Claims,
    IReadOnlyCollection<EligibilitySnapshot> Eligibility,
    IReadOnlyCollection<InvestigationSnapshot> Investigations,
    IReadOnlyCollection<DecisionSnapshot> Decisions,
    IReadOnlyCollection<ResolutionSnapshot> Resolutions);
