using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;
using AfriWallet.Compliance.IdentityVerification.Domain.Providers;
using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Infrastructure.Providers;

public sealed class SandboxVerificationProvider : IVerificationProvider
{
    private readonly VerificationType _supportedTypes;

    public VerificationProvider Descriptor { get; }

    public SandboxVerificationProvider(string code, string displayName, VerificationType supportedTypes)
    {
        Descriptor = new VerificationProvider(code, displayName, Sandbox: true, ProviderStatus.Healthy);
        _supportedTypes = supportedTypes;
    }

    public bool Supports(VerificationType type) =>
        type != VerificationType.None && (_supportedTypes & type) == type;

    public Task<ProviderSubmissionResult> SubmitAsync(VerificationSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reference = $"sandbox-{Descriptor.Code.ToLowerInvariant()}-{Guid.NewGuid():N}";
        return Task.FromResult(new ProviderSubmissionResult(reference, Accepted: true));
    }
}
