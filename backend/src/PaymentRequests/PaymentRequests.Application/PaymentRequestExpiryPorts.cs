using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestDueReader
{
    Task<IReadOnlyList<PaymentRequest>> ListDueAsync(
        DateTimeOffset asOfUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
