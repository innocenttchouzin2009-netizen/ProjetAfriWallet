using MerchantAcquiring.Domain.Payments;
using MerchantAcquiring.Domain.Profiles;
using MerchantAcquiring.Domain.Refunds;

namespace MerchantAcquiring.Application.Interfaces;

public interface IMerchantAcquiringRepository
{
    Task AddProfileAsync(
        MerchantAcquiringProfile profile,
        CancellationToken cancellationToken);

    Task<MerchantAcquiringProfile?> GetProfileAsync(
        string merchantId,
        CancellationToken cancellationToken);

    Task AddPaymentAsync(
        AcquiringPayment payment,
        CancellationToken cancellationToken);

    Task<AcquiringPayment?> GetPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken);

    Task<AcquiringPayment?> GetPaymentByIdempotencyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task AddRefundAsync(
        MerchantRefund refund,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MerchantRefund>>
        GetRefundsAsync(
            Guid paymentId,
            CancellationToken cancellationToken);
}
