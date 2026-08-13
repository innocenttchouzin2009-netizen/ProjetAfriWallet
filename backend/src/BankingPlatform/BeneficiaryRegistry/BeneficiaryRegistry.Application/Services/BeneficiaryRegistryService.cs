using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Interfaces;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Accounts;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Beneficiaries;

namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Services;

public sealed class BeneficiaryRegistryService
{
    private readonly IBeneficiaryRepository _repository;

    public BeneficiaryRegistryService(IBeneficiaryRepository repository)
    {
        _repository = repository;
    }

    public async Task<BankBeneficiary> CreateBeneficiaryAsync(
        CreateBeneficiaryRequest request,
        CancellationToken cancellationToken)
    {
        var beneficiary = new BankBeneficiary(
            Guid.NewGuid(),
            request.OwnerAwid,
            request.DisplayName,
            request.Type);

        await _repository.AddAsync(beneficiary, cancellationToken);

        return beneficiary;
    }

    public async Task<ExternalBankAccount> AddBankAccountAsync(
        AddBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var beneficiary = await _repository.GetAsync(request.BeneficiaryId, cancellationToken)
            ?? throw new KeyNotFoundException("Beneficiary not found.");

        var identifier = new BankAccountIdentifier(request.IdentifierType, request.IdentifierValue);

        var duplicate = await _repository.FindByBankIdentifierAsync(
            beneficiary.OwnerAwid,
            identifier.Value,
            cancellationToken);

        if (duplicate is not null)
        {
            throw new InvalidOperationException("Bank account identifier already registered for this owner.");
        }

        var account = new ExternalBankAccount(
            Guid.NewGuid(),
            identifier,
            request.BankName,
            request.CountryCode,
            request.CurrencyCode,
            request.AccountHolderName);

        beneficiary.AddAccount(account);

        return account;
    }

    public async Task VerifyBankAccountAsync(
        Guid beneficiaryId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var beneficiary = await _repository.GetAsync(beneficiaryId, cancellationToken)
            ?? throw new KeyNotFoundException("Beneficiary not found.");

        var account = beneficiary.Accounts.FirstOrDefault(x => x.BankAccountId == bankAccountId)
            ?? throw new KeyNotFoundException("Bank account not found.");

        account.Verify();
    }

    public async Task<BeneficiaryView?> GetAsync(
        Guid beneficiaryId,
        CancellationToken cancellationToken)
    {
        var beneficiary = await _repository.GetAsync(beneficiaryId, cancellationToken);

        return beneficiary is null ? null : Map(beneficiary);
    }

    public async Task<IReadOnlyCollection<BeneficiaryView>> ListByOwnerAsync(
        string ownerAwid,
        CancellationToken cancellationToken)
    {
        var items = await _repository.ListByOwnerAsync(ownerAwid, cancellationToken);

        return items.Select(Map).ToArray();
    }

    private static BeneficiaryView Map(BankBeneficiary beneficiary)
    {
        return new BeneficiaryView(
            beneficiary.BeneficiaryId,
            beneficiary.OwnerAwid,
            beneficiary.DisplayName,
            beneficiary.Type.ToString(),
            beneficiary.Status.ToString(),
            beneficiary.Accounts.Select(account =>
                new BankAccountView(
                    account.BankAccountId,
                    account.BankName,
                    account.CountryCode,
                    account.CurrencyCode,
                    account.AccountHolderName,
                    account.Identifier.MaskedValue,
                    account.Status.ToString())).ToArray());
    }
}
