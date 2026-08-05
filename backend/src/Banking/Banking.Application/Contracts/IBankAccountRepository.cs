using AfriWallet.Banking.Domain.Entities;

namespace AfriWallet.Banking.Application.Contracts;

public interface IBankAccountRepository
{
    Task<BankAccount> CreateAsync(BankAccount account, CancellationToken cancellationToken = default);
    Task<BankAccount?> GetByIdAsync(string bankAccountId, CancellationToken cancellationToken = default);
    Task<BankAccount?> UpdateAsync(BankAccount account, CancellationToken cancellationToken = default);
    Task<BankAccount?> FindByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default);
}
