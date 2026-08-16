using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

public interface IVerificationSessionRepository
{
    Task<VerificationSession?> GetAsync(VerificationSessionId id, CancellationToken cancellationToken = default);
    Task<VerificationSession?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default);
    Task SaveAsync(VerificationSession session, CancellationToken cancellationToken = default);
}
