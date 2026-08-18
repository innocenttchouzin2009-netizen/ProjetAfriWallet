namespace AfriWallet.Disputes.Resolution.Domain.Compensation;

public sealed record CompensationRecord(
    Guid CompensationId,
    string Reason,
    string? ProviderReference,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? CompletedAtUtc);
