using IdentityService.Application.Abstractions;
using IdentityService.Domain.Entities;

namespace IdentityService.Infrastructure.Repositories;

public sealed class InMemoryAwidRepository : IAwidRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Awid> _awidsById = new();
    private readonly Dictionary<string, Awid> _awidsBySubjectId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Awid> _awidsByCanonicalAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Awid> _awidsByPublicAwid = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _reservedAliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AwidAliasHistoryEntry> _aliasHistory = new();

    public Task<Awid?> GetBySubjectIdAsync(string subjectId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _awidsBySubjectId.TryGetValue(subjectId, out var awid);
            return Task.FromResult(awid);
        }
    }

    public Task<Awid?> GetByCanonicalAliasAsync(string aliasCanonical, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _awidsByCanonicalAlias.TryGetValue(aliasCanonical, out var awid);
            return Task.FromResult(awid);
        }
    }

    public Task<Awid?> GetByPublicAwidAsync(string publicAwid, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _awidsByPublicAwid.TryGetValue(publicAwid, out var awid);
            return Task.FromResult(awid);
        }
    }

    public Task<bool> IsAliasAvailableAsync(string aliasCanonical, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var available = IsAliasAvailableInternal(aliasCanonical, DateTimeOffset.UtcNow);
            return Task.FromResult(available);
        }
    }

    public Task<AwidCreateResult> TryCreateAsync(Awid awid, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_awidsBySubjectId.ContainsKey(awid.SubjectId))
            {
                return Task.FromResult(new AwidCreateResult(false, AwidCreateFailureReason.SubjectAlreadyExists));
            }

            if (_awidsByPublicAwid.ContainsKey(awid.PublicAwid))
            {
                return Task.FromResult(new AwidCreateResult(false, AwidCreateFailureReason.PublicAwidAlreadyExists));
            }

            if (!IsAliasAvailableInternal(awid.AliasCanonical, DateTimeOffset.UtcNow))
            {
                return Task.FromResult(new AwidCreateResult(false, AwidCreateFailureReason.AliasUnavailable));
            }

            _awidsById[awid.Id] = awid;
            _awidsBySubjectId[awid.SubjectId] = awid;
            _awidsByCanonicalAlias[awid.AliasCanonical] = awid;
            _awidsByPublicAwid[awid.PublicAwid] = awid;

            return Task.FromResult(new AwidCreateResult(true, AwidCreateFailureReason.None));
        }
    }

    public Task<AwidAliasChangeResult> TryChangeAliasAsync(string subjectId, string newAliasCanonical, DateTimeOffset changedAt, TimeSpan cooldown, TimeSpan oldAliasReservation, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_awidsBySubjectId.TryGetValue(subjectId, out var awid))
            {
                return Task.FromResult(new AwidAliasChangeResult(false, AwidAliasChangeFailureReason.AwidNotFound));
            }

            if (!string.Equals(awid.AliasCanonical, newAliasCanonical, StringComparison.OrdinalIgnoreCase))
            {
                if (awid.LastAliasChangedAt is not null && awid.LastAliasChangedAt.Value + cooldown > changedAt)
                {
                    return Task.FromResult(new AwidAliasChangeResult(false, AwidAliasChangeFailureReason.CooldownNotReached));
                }

                if (!IsAliasAvailableInternal(newAliasCanonical, changedAt))
                {
                    return Task.FromResult(new AwidAliasChangeResult(false, AwidAliasChangeFailureReason.AliasUnavailable));
                }

                var oldAlias = awid.AliasCanonical;
                _awidsByCanonicalAlias.Remove(oldAlias);
                _reservedAliases[oldAlias] = changedAt + oldAliasReservation;

                _awidsByCanonicalAlias[newAliasCanonical] = awid;
                awid.AliasCanonical = newAliasCanonical;
                awid.AliasDisplay = $"@{newAliasCanonical}";
                awid.LastAliasChangedAt = changedAt;
                awid.UpdatedAt = changedAt;
                awid.Version += 1;

                _aliasHistory.Add(new AwidAliasHistoryEntry
                {
                    AwidId = awid.Id,
                    PreviousAlias = oldAlias,
                    NewAlias = newAliasCanonical,
                    ChangedAt = changedAt,
                    ReservedUntil = changedAt + oldAliasReservation
                });
            }

            return Task.FromResult(new AwidAliasChangeResult(true, AwidAliasChangeFailureReason.None, awid));
        }
    }

    public Task<IReadOnlyList<AwidAliasHistoryEntry>> ListAliasHistoryAsync(string subjectId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_awidsBySubjectId.TryGetValue(subjectId, out var awid))
            {
                return Task.FromResult<IReadOnlyList<AwidAliasHistoryEntry>>(Array.Empty<AwidAliasHistoryEntry>());
            }

            var entries = _aliasHistory.Where(x => x.AwidId == awid.Id).ToList();
            return Task.FromResult<IReadOnlyList<AwidAliasHistoryEntry>>(entries);
        }
    }

    private bool IsAliasAvailableInternal(string aliasCanonical, DateTimeOffset now)
    {
        if (_awidsByCanonicalAlias.ContainsKey(aliasCanonical))
        {
            return false;
        }

        if (_reservedAliases.TryGetValue(aliasCanonical, out var reservedUntil) && reservedUntil > now)
        {
            return false;
        }

        return true;
    }

    public Task<IReadOnlyList<Awid>> ListAllAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<Awid>>(_awidsById.Values.ToList());
        }
    }
}
