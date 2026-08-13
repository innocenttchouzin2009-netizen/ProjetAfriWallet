using System.Collections.Concurrent;
using MerchantAcquiring.Application.Interfaces;
using MerchantAcquiring.Domain.Payments;
using MerchantAcquiring.Domain.Profiles;
using MerchantAcquiring.Domain.Refunds;

namespace MerchantAcquiring.Infrastructure.Repositories;

public sealed class InMemoryMerchantAcquiringRepository :
    IMerchantAcquiringRepository
{
    private readonly ConcurrentDictionary<
        string,
        MerchantAcquiringProfile> _profiles =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<
        Guid,
        AcquiringPayment> _payments = new();

    private readonly ConcurrentDictionary<
        Guid,
        MerchantRefund> _refunds = new();

    public Task AddProfileAsync(
        MerchantAcquiringProfile profile,
        CancellationToken cancellationToken)
    {
        if (!_profiles.TryAdd(
                profile.MerchantId,
                profile))
        {
            throw new InvalidOperationException(
                "Merchant acquiring profile already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<MerchantAcquiringProfile?> GetProfileAsync(
        string merchantId,
        CancellationToken cancellationToken)
    {
        _profiles.TryGetValue(
            merchantId,
            out var profile);

        return Task.FromResult(profile);
    }

    public Task AddPaymentAsync(
        AcquiringPayment payment,
        CancellationToken cancellationToken)
    {
        if (_payments.Values.Any(x =>
                x.IdempotencyKey ==
                payment.IdempotencyKey))
        {
            throw new InvalidOperationException(
                "Payment idempotency key already exists.");
        }

        if (!_payments.TryAdd(
                payment.PaymentId,
                payment))
        {
            throw new InvalidOperationException(
                "Payment already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<AcquiringPayment?> GetPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        _payments.TryGetValue(
            paymentId,
            out var payment);

        return Task.FromResult(payment);
    }

    public Task<AcquiringPayment?>
        GetPaymentByIdempotencyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _payments.Values.FirstOrDefault(x =>
                x.IdempotencyKey ==
                idempotencyKey));
    }

    public Task AddRefundAsync(
        MerchantRefund refund,
        CancellationToken cancellationToken)
    {
        if (!_refunds.TryAdd(
                refund.RefundId,
                refund))
        {
            throw new InvalidOperationException(
                "Refund already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<MerchantRefund>>
        GetRefundsAsync(
            Guid paymentId,
            CancellationToken cancellationToken)
    {
        return Task.FromResult<
            IReadOnlyCollection<MerchantRefund>>(
            _refunds.Values
                .Where(x =>
                    x.PaymentId == paymentId)
                .ToArray());
    }
}
