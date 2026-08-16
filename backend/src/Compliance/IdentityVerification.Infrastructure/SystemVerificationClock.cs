using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

namespace AfriWallet.Compliance.IdentityVerification.Infrastructure;

public sealed class SystemVerificationClock : IVerificationClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
