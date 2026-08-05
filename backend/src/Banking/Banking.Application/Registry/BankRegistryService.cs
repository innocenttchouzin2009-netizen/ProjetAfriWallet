using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Domain.Entities;

namespace AfriWallet.Banking.Application.Registry;

public sealed class BankRegistryService
{
    private readonly IBankProviderRepository _repository;

    public BankRegistryService(IBankProviderRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<BankProvider>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<BankProvider?> GetByIdAsync(string providerId, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(providerId, cancellationToken);

    public Task<IReadOnlyList<BankProvider>> SearchAsync(string? country, string? currency, string? scheme, string? environment, CancellationToken cancellationToken = default)
        => _repository.SearchAsync(country, currency, scheme, environment, cancellationToken);

    public Task<BankProvider> CreateAsync(BankProvider provider, CancellationToken cancellationToken = default)
        => _repository.CreateAsync(provider, cancellationToken);

    public Task<BankProvider?> UpdateAsync(BankProvider provider, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(provider, cancellationToken);
}
