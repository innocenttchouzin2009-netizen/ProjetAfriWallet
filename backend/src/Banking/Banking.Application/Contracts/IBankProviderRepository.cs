using AfriWallet.Banking.Domain.Entities;

namespace AfriWallet.Banking.Application.Contracts;

public interface IBankProviderRepository
{
    Task<IReadOnlyList<BankProvider>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BankProvider?> GetByIdAsync(string providerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankProvider>> SearchAsync(string? country, string? currency, string? scheme, string? environment, CancellationToken cancellationToken = default);
    Task<BankProvider> CreateAsync(BankProvider provider, CancellationToken cancellationToken = default);
    Task<BankProvider?> UpdateAsync(BankProvider provider, CancellationToken cancellationToken = default);
}
