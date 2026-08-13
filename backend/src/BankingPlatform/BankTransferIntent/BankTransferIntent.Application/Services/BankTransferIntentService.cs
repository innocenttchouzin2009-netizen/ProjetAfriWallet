using AfriWallet.BankingPlatform.BankTransferIntent.Application.Interfaces;
using AfriWallet.BankingPlatform.BankTransferIntent.Domain.Money;
using TransferIntent = AfriWallet.BankingPlatform.BankTransferIntent.Domain.Transfers.BankTransferIntent;

namespace AfriWallet.BankingPlatform.BankTransferIntent.Application.Services;

public sealed class BankTransferIntentService
{
    private readonly IBankTransferIntentRepository _repository;
    private readonly IBeneficiaryRegistryGateway _beneficiaries;

    public BankTransferIntentService(
        IBankTransferIntentRepository repository,
        IBeneficiaryRegistryGateway beneficiaries)
    {
        _repository = repository;
        _beneficiaries = beneficiaries;
    }

    public async Task<TransferIntent> CreateAsync(
        CreateBankTransferIntentRequest request,
        CancellationToken cancellationToken)
    {
        var existing =
            await _repository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
            return existing;

        if (request.LifetimeMinutes is <= 0 or > 1440)
            throw new ArgumentOutOfRangeException(
                nameof(request.LifetimeMinutes),
                "Transfer intent lifetime must be between 1 minute and 24 hours.");

        var eligibility =
            await _beneficiaries.GetEligibilityAsync(
                request.OwnerAwid,
                request.BeneficiaryId,
                request.BankAccountId,
                cancellationToken);

        if (!eligibility.Exists)
            throw new InvalidOperationException(
                "Beneficiary bank account does not exist.");

        if (!eligibility.BeneficiaryActive)
            throw new InvalidOperationException(
                "Beneficiary is not active.");

        if (!eligibility.BankAccountVerified)
            throw new InvalidOperationException(
                "Bank account is not verified.");

        if (!string.Equals(
                eligibility.CurrencyCode,
                request.CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Transfer currency does not match beneficiary bank account currency.");
        }

        var transferIntent =
            new TransferIntent(
                Guid.NewGuid(),
                request.OwnerAwid,
                request.BeneficiaryId,
                request.BankAccountId,
                new MoneyAmount(
                    request.AmountMinor,
                    request.CurrencyCode),
                request.Reference,
                request.IdempotencyKey,
                DateTime.UtcNow.AddMinutes(
                    request.LifetimeMinutes));

        await _repository.AddAsync(
            transferIntent,
            cancellationToken);

        return transferIntent;
    }

    public async Task<TransferIntent> ConfirmAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken)
    {
        var transfer =
            await RequireAsync(
                transferIntentId,
                cancellationToken);

        transfer.Confirm();

        return transfer;
    }

    public async Task<TransferIntent> MarkReadyForRoutingAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken)
    {
        var transfer =
            await RequireAsync(
                transferIntentId,
                cancellationToken);

        transfer.MarkReadyForRouting();

        return transfer;
    }

    public async Task<TransferIntent> CancelAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken)
    {
        var transfer =
            await RequireAsync(
                transferIntentId,
                cancellationToken);

        transfer.Cancel();

        return transfer;
    }

    public Task<TransferIntent?> GetAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken)
    {
        return _repository.GetAsync(
            transferIntentId,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<TransferIntent>> ListByOwnerAsync(
        string ownerAwid,
        CancellationToken cancellationToken)
    {
        return _repository.ListByOwnerAsync(
            ownerAwid,
            cancellationToken);
    }

    private async Task<TransferIntent> RequireAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(
                   transferIntentId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Bank transfer intent not found.");
    }
}
