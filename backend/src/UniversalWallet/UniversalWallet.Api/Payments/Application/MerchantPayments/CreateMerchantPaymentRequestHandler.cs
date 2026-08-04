using UniversalWallet.Api.Payments.Domain.MerchantPayments;

namespace UniversalWallet.Api.Payments.Application.MerchantPayments;

public sealed class CreateMerchantPaymentRequestHandler
{
    private readonly IMerchantProfileRepository _merchantRepository;
    private readonly IMerchantPaymentRequestRepository _requestRepository;
    private readonly IMerchantQrTokenRepository _qrRepository;

    public CreateMerchantPaymentRequestHandler(
        IMerchantProfileRepository merchantRepository,
        IMerchantPaymentRequestRepository requestRepository,
        IMerchantQrTokenRepository qrRepository)
    {
        _merchantRepository = merchantRepository;
        _requestRepository = requestRepository;
        _qrRepository = qrRepository;
    }

    public async Task<CreateMerchantPaymentRequestResponse> HandleAsync(Guid merchantAwid, CreateMerchantPaymentRequestRequest request, CancellationToken cancellationToken = default)
    {
        var merchant = await _merchantRepository.GetByAwidAsync(merchantAwid, cancellationToken);
        if (merchant is null || merchant.Status != MerchantStatus.Active)
        {
            throw new InvalidOperationException("MERCHANT_NOT_ACTIVE");
        }

        var paymentRequest = new MerchantPaymentRequest
        {
            MerchantId = merchant.Id,
            MerchantWalletId = merchant.SettlementWalletId,
            AmountMinor = request.AmountMinor,
            CurrencyCode = request.CurrencyCode.ToUpperInvariant(),
            Description = request.Description,
            ExternalReference = request.ExternalReference,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(request.ExpiresInSeconds),
            Status = MerchantPaymentRequestStatus.Active,
            Version = 1
        };

        await _requestRepository.AddAsync(paymentRequest, cancellationToken);

        var qrToken = new MerchantQrToken
        {
            MerchantId = merchant.Id,
            Type = MerchantQrType.MerchantDynamic,
            Token = $"AQR_{Guid.NewGuid():N}",
            PaymentRequestId = paymentRequest.Id,
            IsActive = true,
            ExpiresAt = paymentRequest.ExpiresAt,
            MaxUses = 1,
            UseCount = 0
        };

        await _qrRepository.AddAsync(qrToken, cancellationToken);

        return new CreateMerchantPaymentRequestResponse(paymentRequest.Id, paymentRequest.AmountMinor, paymentRequest.CurrencyCode, paymentRequest.Description, paymentRequest.Status);
    }
}
