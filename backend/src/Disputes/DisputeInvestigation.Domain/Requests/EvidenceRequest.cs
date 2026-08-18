using AfriWallet.Disputes.Investigation.Domain.Evidence;

namespace AfriWallet.Disputes.Investigation.Domain.Requests;

public sealed record EvidenceRequest(
    Guid RequestId,
    EvidenceType RequestedType,
    string RequestedFrom,
    string Reason,
    EvidenceRequestStatus Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? FulfilledAtUtc);
