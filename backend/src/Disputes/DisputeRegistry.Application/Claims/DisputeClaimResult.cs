using AfriWallet.Disputes.Registry.Domain.Claims;

namespace AfriWallet.Disputes.Registry.Application.Claims;

public sealed record DisputeClaimResult(
    Guid ClaimId,
    string Awid,
    Guid TransactionId,
    DisputeClaimType Type,
    string Reason,
    long AmountMinor,
    string Currency,
    DisputeClaimStatus Status,
    DisputeSourceChannel SourceChannel,
    string? Outcome,
    int EvidenceCount,
    int HistoryCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc);
