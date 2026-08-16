namespace AfriWallet.Compliance.Screening.Application.Abstractions;

public interface IScreeningProviderRegistry
{
    IReadOnlyCollection<IScreeningListProvider> All();
}