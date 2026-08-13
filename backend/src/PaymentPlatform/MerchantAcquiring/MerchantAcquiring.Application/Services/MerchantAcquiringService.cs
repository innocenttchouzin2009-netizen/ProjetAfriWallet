using MerchantAcquiring.Application.Interfaces;
using MerchantAcquiring.Domain.Payments;
using MerchantAcquiring.Domain.Profiles;
using MerchantAcquiring.Domain.Refunds;

namespace MerchantAcquiring.Application.Services;

public sealed class MerchantAcquiringService
{
    private readonly IMerchantAcquiringRepository _repository;
    private readonly IMerchantRegistryGateway _merchantRegistry;
    private readonly IPaymentRoutingGateway _routing;
    private readonly IAcquiringProcessorGateway _processor;
    private readonly AcquiringFeeCalculator _fees;

    public MerchantAcquiringService(
        IMerchantAcquiringRepository repository,
        IMerchantRegistryGateway merchantRegistry,
        IPaymentRoutingGateway routing,
        IAcquiringProcessorGateway processor,
        AcquiringFeeCalculator fees)
    {
        _repository = repository;
        _merchantRegistry = merchantRegistry;
        _routing = routing;
        _processor = processor;
        _fees = fees;
    }

    public async Task<MerchantAcquiringProfile>
        CreateProfileAsync(
            string merchantId,
            string countryCode,
            string settlementCurrency,
            CancellationToken cancellationToken)
    {
        var active =
            await _merchantRegistry.IsMerchantActiveAsync(
                merchantId,
                cancellationToken);

        if (!active)
            throw new InvalidOperationException(
                "Merchant is not eligible for acquiring.");

        var existing =
            await _repository.GetProfileAsync(
                merchantId,
                cancellationToken);

        if (existing is not null)
            return existing;

        var profile =
            new MerchantAcquiringProfile(
                Guid.NewGuid(),
                merchantId,
                countryCode,
                settlementCurrency);

        await _repository.AddProfileAsync(
            profile,
            cancellationToken);

        return profile;
    }

    public async Task<AcquiringPayment>
        CreatePaymentAsync(
            Guid paymentIntentId,
            string merchantId,
            string currencyCode,
            long amountMinor,
            AcquiringPaymentMethod method,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        var existing =
            await _repository
                .GetPaymentByIdempotencyAsync(
                    idempotencyKey,
                    cancellationToken);

        if (existing is not null)
            return existing;

        var profile =
            await _repository.GetProfileAsync(
                merchantId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Merchant acquiring profile not found.");

        if (!profile.Supports(
                currencyCode,
                method))
        {
            throw new InvalidOperationException(
                "Payment method or currency is not enabled.");
        }

        var payment =
            new AcquiringPayment(
                Guid.NewGuid(),
                paymentIntentId,
                merchantId,
                currencyCode,
                amountMinor,
                idempotencyKey);

        var fee =
            _fees.Calculate(
                amountMinor,
                profile.PercentageFee,
                profile.FixedFeeMinor);

        payment.ApplyFee(fee);

        await _repository.AddPaymentAsync(
            payment,
            cancellationToken);

        return payment;
    }

    public async Task<AcquiringPayment>
        AuthorizeAsync(
            Guid paymentId,
            string countryCode,
            CancellationToken cancellationToken)
    {
        var payment =
            await RequirePaymentAsync(
                paymentId,
                cancellationToken);

        var route =
            await _routing.RouteAsync(
                payment.PaymentIntentId,
                countryCode,
                payment.CurrencyCode,
                payment.AmountMinor,
                cancellationToken);

        payment.Route(
            route.ProviderId);

        var providerReference =
            await _processor.AuthorizeAsync(
                payment.PaymentId,
                route.ProviderId,
                payment.AmountMinor,
                payment.CurrencyCode,
                cancellationToken);

        payment.Authorize(
            providerReference);

        return payment;
    }

    public async Task<AcquiringPayment>
        CaptureAsync(
            Guid paymentId,
            CancellationToken cancellationToken)
    {
        var payment =
            await RequirePaymentAsync(
                paymentId,
                cancellationToken);

        if (payment.ProviderReference is null)
            throw new InvalidOperationException(
                "Provider authorization is missing.");

        await _processor.CaptureAsync(
            payment.ProviderReference,
            cancellationToken);

        payment.Capture();

        return payment;
    }

    public async Task<MerchantRefund>
        RefundAsync(
            Guid paymentId,
            long amountMinor,
            string reason,
            CancellationToken cancellationToken)
    {
        var payment =
            await RequirePaymentAsync(
                paymentId,
                cancellationToken);

        if (payment.Status !=
            AcquiringPaymentStatus.Captured)
        {
            throw new InvalidOperationException(
                "Only captured payments may be refunded.");
        }

        var previousRefunds =
            await _repository.GetRefundsAsync(
                paymentId,
                cancellationToken);

        var alreadyRefunded =
            previousRefunds
                .Where(x =>
                    x.Status ==
                    RefundStatus.Completed)
                .Sum(x => x.AmountMinor);

        if (alreadyRefunded +
            amountMinor >
            payment.AmountMinor)
        {
            throw new InvalidOperationException(
                "Refund exceeds captured amount.");
        }

        if (payment.ProviderReference is null)
            throw new InvalidOperationException(
                "Provider reference is missing.");

        await _processor.RefundAsync(
            payment.ProviderReference,
            amountMinor,
            cancellationToken);

        var refund =
            new MerchantRefund(
                Guid.NewGuid(),
                paymentId,
                amountMinor,
                reason);

        refund.Complete();

        await _repository.AddRefundAsync(
            refund,
            cancellationToken);

        return refund;
    }

    private async Task<AcquiringPayment>
        RequirePaymentAsync(
            Guid paymentId,
            CancellationToken cancellationToken)
    {
        return await _repository.GetPaymentAsync(
                   paymentId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Acquiring payment not found.");
    }
}
