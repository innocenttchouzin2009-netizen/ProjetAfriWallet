namespace AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

[Flags]
public enum VerificationType
{
    None = 0,
    Document = 1,
    Selfie = 2,
    Liveness = 4
}
