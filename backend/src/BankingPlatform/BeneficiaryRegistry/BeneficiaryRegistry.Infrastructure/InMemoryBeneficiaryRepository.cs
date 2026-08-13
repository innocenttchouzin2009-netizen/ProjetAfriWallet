using System.Collections.Concurrent;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Interfaces;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Beneficiaries;

namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Infrastructure;

public sealed class InMemoryBeneficiaryRepository : IBeneficiaryRepository
{
    private readonly ConcurrentDictionary<Guid, BankBeneficiary> _items = new();

    public Task AddAsync(BankBeneficiary beneficiary, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_items.TryAdd(beneficiary.BeneficiaryId, beneficiary))
        {
            throw new InvalidOperationException("Beneficiary already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<BankBeneficiary?> GetAsync(Guid beneficiaryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _items.TryGetValue(beneficiaryId, out var beneficiary);

        return Task.FromResult(beneficiary);
    }

    public Task<IReadOnlyCollection<BankBeneficiary>> ListByOwnerAsync(
        string ownerAwid,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<BankBeneficiary>>(
            _items.Values
                .Where(x => string.Equals(x.OwnerAwid, ownerAwid, StringComparison.OrdinalIgnoreCase))
                .ToArray());
    }

    public Task<BankBeneficiary?> FindByBankIdentifierAsync(
        string ownerAwid,
        string normalizedIdentifier,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result =
            _items.Values
                .Where(x => string.Equals(x.OwnerAwid, ownerAwid, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(x => x.Accounts.Any(a =>
                    string.Equals(a.Identifier.Value, normalizedIdentifier, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(result);
    }
}
