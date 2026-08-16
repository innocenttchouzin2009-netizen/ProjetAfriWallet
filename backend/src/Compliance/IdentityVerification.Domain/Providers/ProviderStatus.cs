namespace AfriWallet.Compliance.IdentityVerification.Domain.Providers;

public enum ProviderStatus
{
    Healthy = 0,
    Degraded = 1,
    Unavailable = 2,
    Disabled = 3
}
