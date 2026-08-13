using TransferIntent = AfriWallet.BankingPlatform.BankTransferIntent.Domain.Transfers.BankTransferIntent;

namespace AfriWallet.BankingPlatform.BankTransferIntent.Application.Interfaces;

public interface IBankTransferIntentRepository
{
    Task AddAsync(
        TransferIntent transferIntent,
        CancellationToken cancellationToken);

    Task<TransferIntent?> GetAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken);

    Task<TransferIntent?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TransferIntent>> ListByOwnerAsync(
        string ownerAwid,
        CancellationToken cancellationToken);
}
