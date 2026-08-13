using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Providers;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

public interface IBankProviderRegistry
{
    BankProviderDefinition GetRequired(string providerCode);
    IBankProviderAdapter GetAdapter(string providerCode);
    IReadOnlyCollection<BankProviderDefinition> List();
}
