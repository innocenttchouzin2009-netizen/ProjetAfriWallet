using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Abstractions;

public interface IMobileMoneyPayoutExecutionBindingStore
{
    Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default);

    async Task SaveOwnedAsync(
        Guid ownerUserId,
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        await SaveAsync(binding, cancellationToken);
    }

    Task<Guid?> FindOwnerUserIdAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);
}
