using AfriWallet.Compliance.IdentityVerification.Domain.Providers;
using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

public interface IVerificationProvider
{
    VerificationProvider Descriptor { get; }
    bool Supports(VerificationType type);
    Task<ProviderSubmissionResult> SubmitAsync(VerificationSession session, CancellationToken cancellationToken = default);
}

public sealed record ProviderSubmissionResult(
    string ProviderReference,
    bool Accepted);
