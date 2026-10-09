using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Abstractions;

public interface IMobileMoneyPayoutExecutionIdempotencyStore
{
    Task<MobileMoneyPayoutExecutionIdempotencyEntry?> FindAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        MobileMoneyPayoutExecutionIdempotencyEntry entry,
        CancellationToken cancellationToken = default);
}
