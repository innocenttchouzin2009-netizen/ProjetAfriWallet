using AfriWallet.Disputes.Registry.Domain.Claims;

namespace AfriWallet.Disputes.Registry.Domain.History;

public sealed record DisputeClaimHistoryEntry(
    Guid EntryId,
    DisputeClaimStatus FromStatus,
    DisputeClaimStatus ToStatus,
    string Actor,
    string Reason,
    DateTimeOffset OccurredAtUtc);
