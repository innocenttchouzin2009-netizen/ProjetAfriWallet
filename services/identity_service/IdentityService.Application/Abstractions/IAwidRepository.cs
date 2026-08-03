using IdentityService.Domain.Entities;

namespace IdentityService.Application.Abstractions;

public interface IAwidRepository
{
    Task<Awid?> GetBySubjectIdAsync(string subjectId, CancellationToken cancellationToken);
    Task<Awid?> GetByCanonicalAliasAsync(string aliasCanonical, CancellationToken cancellationToken);
    Task<Awid?> GetByPublicAwidAsync(string publicAwid, CancellationToken cancellationToken);
    Task<bool> IsAliasAvailableAsync(string aliasCanonical, CancellationToken cancellationToken);
    Task<AwidCreateResult> TryCreateAsync(Awid awid, CancellationToken cancellationToken);
    Task<AwidAliasChangeResult> TryChangeAliasAsync(string subjectId, string newAliasCanonical, DateTimeOffset changedAt, TimeSpan cooldown, TimeSpan oldAliasReservation, CancellationToken cancellationToken);
    Task<IReadOnlyList<AwidAliasHistoryEntry>> ListAliasHistoryAsync(string subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Awid>> ListAllAsync(CancellationToken cancellationToken);
}

public sealed record AwidCreateResult(bool Success, AwidCreateFailureReason FailureReason);

public enum AwidCreateFailureReason
{
    None,
    SubjectAlreadyExists,
    AliasUnavailable,
    PublicAwidAlreadyExists
}

public sealed record AwidAliasChangeResult(bool Success, AwidAliasChangeFailureReason FailureReason, Awid? Awid = null);

public enum AwidAliasChangeFailureReason
{
    None,
    AwidNotFound,
    AliasUnavailable,
    CooldownNotReached
}
