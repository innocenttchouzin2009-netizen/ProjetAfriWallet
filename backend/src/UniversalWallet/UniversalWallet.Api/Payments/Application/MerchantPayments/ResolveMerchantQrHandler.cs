using UniversalWallet.Api.Payments.Domain.MerchantPayments;

namespace UniversalWallet.Api.Payments.Application.MerchantPayments;

public sealed class ResolveMerchantQrHandler
{
    private readonly IMerchantQrTokenRepository _qrRepository;
    private readonly IMerchantProfileRepository _merchantRepository;
    private readonly IMerchantPaymentRequestRepository _requestRepository;

    public ResolveMerchantQrHandler(
        IMerchantQrTokenRepository qrRepository,
        IMerchantProfileRepository merchantRepository,
        IMerchantPaymentRequestRepository requestRepository)
    {
        _qrRepository = qrRepository;
        _merchantRepository = merchantRepository;
        _requestRepository = requestRepository;
    }

    public async Task<ResolveMerchantQrResponse> HandleAsync(ResolveMerchantQrRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _qrRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (token is null || !token.IsActive || token.Revoked)
        {
            throw new InvalidOperationException("MERCHANT_QR_INVALID");
        }

        var merchant = await _merchantRepository.GetAsync(token.MerchantId, cancellationToken);
        if (merchant is null || merchant.Status != MerchantStatus.Active || merchant.VerificationLevel != MerchantVerificationLevel.Verified)
        {
            throw new InvalidOperationException("MERCHANT_NOT_ACTIVE");
        }

        MerchantPaymentRequest? paymentRequest = null;
        if (token.PaymentRequestId is not null)
        {
            paymentRequest = await _requestRepository.GetAsync(token.PaymentRequestId.Value, cancellationToken);
            if (paymentRequest is null || paymentRequest.Status != MerchantPaymentRequestStatus.Active)
            {
                throw new InvalidOperationException("MERCHANT_PAYMENT_REQUEST_NOT_FOUND");
            }
        }

        return new ResolveMerchantQrResponse(
            token.Type.ToString(),
            new MerchantProfileResponse(merchant.DisplayName, $"@{merchant.DisplayName.ToLowerInvariant()}", merchant.VerificationLevel == MerchantVerificationLevel.Verified, merchant.CategoryCode.ToString(), merchant.CountryCode),
            paymentRequest is null ? null : new MerchantPaymentRequestResponse(paymentRequest.Id, paymentRequest.AmountMinor, paymentRequest.CurrencyCode, paymentRequest.Description, paymentRequest.ExpiresAt),
            "CONFIRM_PAYMENT");
    }
}
