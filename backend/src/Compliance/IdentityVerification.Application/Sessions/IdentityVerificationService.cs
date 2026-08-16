using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;
using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Application.Sessions;

public sealed class IdentityVerificationService
{
    private readonly IVerificationSessionRepository _sessions;
    private readonly IVerificationProviderRegistry _providers;
    private readonly IVerificationAuditStore _audit;
    private readonly IVerificationClock _clock;

    public IdentityVerificationService(
        IVerificationSessionRepository sessions,
        IVerificationProviderRegistry providers,
        IVerificationAuditStore audit,
        IVerificationClock clock)
    {
        _sessions = sessions;
        _providers = providers;
        _audit = audit;
        _clock = clock;
    }

    public async Task<VerificationSessionResult> CreateAsync(CreateVerificationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Type == VerificationType.None)
            throw new ArgumentException("Verification type is required.", nameof(command));

        var existing = await _sessions.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var provider = _providers.Resolve(command.ProviderCode);
        if (!provider.Supports(command.Type))
        {
            throw new InvalidOperationException($"Provider '{command.ProviderCode}' does not support {command.Type}.");
        }

        var session = new VerificationSession(
            command.ComplianceProfileId,
            command.Type,
            command.ProviderCode,
            command.IdempotencyKey,
            _clock.UtcNow,
            TimeSpan.FromMinutes(30));

        await _sessions.AddAsync(session, cancellationToken);
        await AuditAsync(session, "identity.verification.created", command.Actor, cancellationToken);

        return Map(session);
    }

    public async Task<VerificationSessionResult> SubmitAsync(Guid sessionId, string actor, CancellationToken cancellationToken = default)
    {
        var session = await RequireAsync(sessionId, cancellationToken);

        var provider = _providers.Resolve(session.ProviderCode);
        var submission = await provider.SubmitAsync(session, cancellationToken);
        session.AttachProviderReference(submission.ProviderReference, _clock.UtcNow);
        session.Submit(_clock.UtcNow);

        await _sessions.SaveAsync(session, cancellationToken);
        await AuditAsync(session, "identity.verification.submitted", actor, cancellationToken);
        return Map(session);
    }

    public async Task<VerificationSessionResult> StartProcessingAsync(Guid sessionId, string actor, CancellationToken cancellationToken = default)
    {
        var session = await RequireAsync(sessionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(session.ProviderReference))
        {
            var provider = _providers.Resolve(session.ProviderCode);
            var submission = await provider.SubmitAsync(session, cancellationToken);
            session.AttachProviderReference(submission.ProviderReference, _clock.UtcNow);
        }

        session.StartProcessing(_clock.UtcNow);

        await _sessions.SaveAsync(session, cancellationToken);
        await AuditAsync(session, "identity.verification.processing", actor, cancellationToken);

        return Map(session);
    }

    public async Task<VerificationSessionResult> CompleteAsync(CompleteVerificationCommand command, CancellationToken cancellationToken = default)
    {
        var session = await RequireAsync(command.SessionId, cancellationToken);
        if (command.Code == string.Empty)
            throw new ArgumentException("Code is required.", nameof(command));

        session.Complete(
            new VerificationResult(
                command.Verified,
                command.Code,
                command.ProviderReference,
                _clock.UtcNow),
            _clock.UtcNow);

        await _sessions.SaveAsync(session, cancellationToken);
        await AuditAsync(
            session,
            command.Verified ? "identity.verification.verified" : "identity.verification.rejected",
            command.Actor,
            cancellationToken);

        return Map(session);
    }

    public async Task<VerificationSessionResult> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Map(await RequireAsync(sessionId, cancellationToken));
    }

    private async Task<VerificationSession> RequireAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(new VerificationSessionId(sessionId), cancellationToken);
        return session ?? throw new KeyNotFoundException("Verification session not found.");
    }

    private async Task AuditAsync(VerificationSession session, string eventType, string actor, CancellationToken cancellationToken)
    {
        await _audit.AppendAsync(
            new VerificationAuditEvent(
                Guid.NewGuid(),
                session.Id.Value,
                session.ComplianceProfileId,
                eventType,
                actor,
                _clock.UtcNow),
            cancellationToken);
    }

    private static VerificationSessionResult Map(VerificationSession session)
    {
        return new VerificationSessionResult(
            session.Id.Value,
            session.ComplianceProfileId,
            session.Type,
            session.ProviderCode,
            session.Status,
            session.ProviderReference,
            session.CreatedAtUtc,
            session.ExpiresAtUtc);
    }
}
