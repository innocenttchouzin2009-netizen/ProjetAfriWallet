namespace AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

public readonly record struct VerificationSessionId(Guid Value)
{
    public static VerificationSessionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
