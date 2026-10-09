using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Abstractions;

public interface IMobileMoneyPayoutStore
{
    Task<MobileMoneyPayout?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<MobileMoneyPayoutCreateOrGetResult> CreateOrGetAsync(
        MobileMoneyPayout candidate,
        RequestFingerprint requestFingerprint,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        MobileMoneyPayout payout,
        CancellationToken cancellationToken = default);
}
