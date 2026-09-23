using AfriWallet.Merchants.Payout.Domain;

namespace AfriWallet.Merchants.Payout.Application;

public interface IMerchantPayoutProviderResultStore
{
    Task SaveAsync(
        MerchantPayoutProviderResultRecord result,
        CancellationToken cancellationToken = default);

    Task<MerchantPayoutProviderResultRecord?> GetAsync(
        Guid resultId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MerchantPayoutProviderResultRecord>> ListForPayoutAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default);
}

public interface IMerchantPayoutReconciliationStore
{
    Task SaveAsync(
        MerchantPayoutReconciliationRecord reconciliation,
        CancellationToken cancellationToken = default);

    Task<MerchantPayoutReconciliationRecord?> GetAsync(
        Guid reconciliationId,
        CancellationToken cancellationToken = default);

    Task<MerchantPayoutReconciliationRecord?> GetLatestForPayoutAsync(
        Guid payoutId,
        CancellationToken cancellationToken = default);

    Task<MerchantPayoutReconciliationRecord?> GetForResultAsync(
        Guid resultId,
        CancellationToken cancellationToken = default);
}
