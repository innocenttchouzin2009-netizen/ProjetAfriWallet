using AfriWallet.Disputes.Resolution.Domain.Resolutions;

namespace AfriWallet.Disputes.Resolution.Application.Results;

public sealed record ResolutionResult(
    Guid ResolutionId,
    Guid DecisionId,
    Guid ClaimId,
    string Awid,
    ResolutionRoute Route,
    ResolutionStatus Status,
    ResolutionReasonCode ReasonCode,
    string IdempotencyKey,
    string? CorrelationId,
    string? ProviderReference,
    int AttemptCount,
    int CompensationCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
