namespace AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

public sealed class VerificationSession
{
    public VerificationSessionId Id { get; }
    public Guid ComplianceProfileId { get; }
    public VerificationType Type { get; }
    public string ProviderCode { get; }
    public string IdempotencyKey { get; }
    public VerificationStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public VerificationResult? Result { get; private set; }

    public VerificationSession(
        Guid complianceProfileId,
        VerificationType type,
        string providerCode,
        string idempotencyKey,
        DateTimeOffset createdAtUtc,
        TimeSpan ttl)
    {
        if (complianceProfileId == Guid.Empty)
            throw new ArgumentException("ComplianceProfileId is required.", nameof(complianceProfileId));
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new ArgumentException("ProviderCode is required.", nameof(providerCode));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("IdempotencyKey is required.", nameof(idempotencyKey));

        Id = VerificationSessionId.New();
        ComplianceProfileId = complianceProfileId;
        Type = type;
        ProviderCode = providerCode;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        ExpiresAtUtc = createdAtUtc.Add(ttl);
        Status = VerificationStatus.Created;
    }

    public void Submit(DateTimeOffset now)
    {
        EnsureNotExpired(now);
        if (Status != VerificationStatus.Created)
            throw new InvalidOperationException("Only created sessions can be submitted.");

        Status = VerificationStatus.Submitted;
        UpdatedAtUtc = now;
    }

    public void StartProcessing(DateTimeOffset now)
    {
        EnsureNotExpired(now);
        if (Status != VerificationStatus.Submitted)
            throw new InvalidOperationException("Only submitted sessions can start processing.");

        Status = VerificationStatus.Processing;
        UpdatedAtUtc = now;
    }

    public void AttachProviderReference(string providerReference, DateTimeOffset now)
    {
        EnsureNotExpired(now);
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new ArgumentException("ProviderReference is required.", nameof(providerReference));

        ProviderReference = providerReference;
        UpdatedAtUtc = now;
    }

    public void Complete(VerificationResult result, DateTimeOffset now)
    {
        EnsureNotExpired(now);
        if (IsTerminal())
            throw new InvalidOperationException("Terminal verification session is immutable.");

        Result = result;
        Status = result.Verified ? VerificationStatus.Verified : VerificationStatus.Rejected;
        UpdatedAtUtc = now;
        ProviderReference ??= result.ProviderReference;
    }

    public void Fail(string code, string? providerReference, DateTimeOffset now)
    {
        EnsureNotExpired(now);
        if (IsTerminal())
            throw new InvalidOperationException("Terminal verification session is immutable.");

        Result = new VerificationResult(false, code, providerReference ?? string.Empty, now);
        Status = VerificationStatus.Failed;
        UpdatedAtUtc = now;
        ProviderReference ??= providerReference;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (IsTerminal())
            throw new InvalidOperationException("Terminal verification session is immutable.");

        Status = VerificationStatus.Cancelled;
        UpdatedAtUtc = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (IsTerminal())
            return;
        if (now < ExpiresAtUtc)
            throw new InvalidOperationException("Session has not expired.");

        Status = VerificationStatus.Expired;
        UpdatedAtUtc = now;
    }

    private void EnsureNotExpired(DateTimeOffset now)
    {
        if (now >= ExpiresAtUtc)
        {
            Status = VerificationStatus.Expired;
            UpdatedAtUtc = now;
            throw new InvalidOperationException("Verification session expired.");
        }
    }

    private bool IsTerminal() =>
        Status is VerificationStatus.Verified or VerificationStatus.Rejected or VerificationStatus.Failed or VerificationStatus.Expired or VerificationStatus.Cancelled;
}
