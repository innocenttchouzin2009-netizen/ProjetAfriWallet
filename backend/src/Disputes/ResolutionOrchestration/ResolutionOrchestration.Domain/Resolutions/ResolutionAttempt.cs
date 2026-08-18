namespace AfriWallet.Disputes.Resolution.Domain.Resolutions;

public sealed record ResolutionAttempt(
    Guid AttemptId,
    int AttemptNumber,
    string CorrelationId,
    string? ProviderReference,
    string Result,
    DateTimeOffset AttemptedAtUtc);
