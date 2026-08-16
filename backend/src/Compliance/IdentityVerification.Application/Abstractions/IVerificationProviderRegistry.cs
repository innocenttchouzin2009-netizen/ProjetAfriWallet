namespace AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

public interface IVerificationProviderRegistry
{
    IVerificationProvider Resolve(string providerCode);
    IReadOnlyCollection<IVerificationProvider> All();
}
