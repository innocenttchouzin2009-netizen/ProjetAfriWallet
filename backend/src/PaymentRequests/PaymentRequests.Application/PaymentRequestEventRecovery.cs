namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestEventRecoveryStore
{
    Task<int> RecoverExpiredClaimsAsync(
        int maxCount,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}
