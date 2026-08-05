using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Domain.Entities;

namespace AfriWallet.Banking.Infrastructure;

public sealed class AccountRepository : IBankAccountRepository
{
    private readonly List<BankAccount> _accounts = [];

    public Task<BankAccount> CreateAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        _accounts.Add(account);
        return Task.FromResult(account);
    }

    public Task<BankAccount?> GetByIdAsync(string bankAccountId, CancellationToken cancellationToken = default)
        => Task.FromResult(_accounts.FirstOrDefault(a => a.BankAccountId.Equals(bankAccountId, StringComparison.OrdinalIgnoreCase)));

    public Task<BankAccount?> UpdateAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        var existing = _accounts.FirstOrDefault(a => a.BankAccountId.Equals(account.BankAccountId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            return Task.FromResult<BankAccount?>(null);
        }

        var index = _accounts.IndexOf(existing);
        _accounts[index] = account;
        return Task.FromResult<BankAccount?>(account);
    }

    public Task<BankAccount?> FindByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
        => Task.FromResult(_accounts.FirstOrDefault(a => a.Fingerprint.Equals(fingerprint, StringComparison.OrdinalIgnoreCase)));
}
