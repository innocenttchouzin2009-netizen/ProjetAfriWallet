namespace AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

public interface IVerificationClock
{
    DateTimeOffset UtcNow { get; }
}
