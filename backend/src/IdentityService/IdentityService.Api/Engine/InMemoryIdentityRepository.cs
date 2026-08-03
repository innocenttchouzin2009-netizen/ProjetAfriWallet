namespace IdentityService.Api.Engine;

public interface IIdentityRepository
{
    IdentityAccount GetOrCreateAccount(string subjectId);
    QrToken? GetPermanentIdentityToken(string subjectId);
    QrToken CreateQrToken(QrToken token);
    QrToken? GetQrTokenById(Guid id);
    void SaveQrToken(QrToken token);
    void RevokeQrToken(Guid id, string subjectId);
    IReadOnlyList<AuditEvent> ListAuditEvents(string subjectId);
    void AddAudit(AuditEvent auditEvent);
}

public sealed class InMemoryIdentityRepository : IIdentityRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, IdentityAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, QrToken> _tokens = new();
    private readonly List<AuditEvent> _auditEvents = [];

    public IdentityAccount GetOrCreateAccount(string subjectId)
    {
        lock (_sync)
        {
            if (_accounts.TryGetValue(subjectId, out var existing))
            {
                return existing;
            }

            var created = new IdentityAccount
            {
                SubjectId = subjectId,
                Alias = "@innocent",
                PublicAwid = "AW-237-K9M4X2Q8",
                DisplayName = "Innocent T.",
                VerificationBadges = ["IDENTITY_VERIFIED"],
                PreferredCurrency = "EUR",
                Theme = "afriwallet-premium",
                PrivacyMode = PrivacyMode.Private,
                Country = "CM",
                BusinessName = "Afri Merchant",
                AssociationName = "AfriCircle Unity",
                BusinessHours = "08:00-18:00"
            };

            _accounts[subjectId] = created;
            return created;
        }
    }

    public QrToken? GetPermanentIdentityToken(string subjectId)
    {
        lock (_sync)
        {
            return _tokens.Values.FirstOrDefault(x =>
                x.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                && x.Type == QrType.Identity
                && x.ExpiresAt is null
                && x.RevokedAt is null);
        }
    }

    public QrToken CreateQrToken(QrToken token)
    {
        lock (_sync)
        {
            _tokens[token.Id] = token;
            return token;
        }
    }

    public QrToken? GetQrTokenById(Guid id)
    {
        lock (_sync)
        {
            _tokens.TryGetValue(id, out var token);
            return token;
        }
    }

    public void SaveQrToken(QrToken token)
    {
        lock (_sync)
        {
            _tokens[token.Id] = token;
        }
    }

    public void RevokeQrToken(Guid id, string subjectId)
    {
        lock (_sync)
        {
            if (_tokens.TryGetValue(id, out var token)
                && token.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase))
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
                _tokens[id] = token;
            }
        }
    }

    public IReadOnlyList<AuditEvent> ListAuditEvents(string subjectId)
    {
        lock (_sync)
        {
            return _auditEvents.Where(x => x.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    public void AddAudit(AuditEvent auditEvent)
    {
        lock (_sync)
        {
            _auditEvents.Add(auditEvent);
        }
    }
}
