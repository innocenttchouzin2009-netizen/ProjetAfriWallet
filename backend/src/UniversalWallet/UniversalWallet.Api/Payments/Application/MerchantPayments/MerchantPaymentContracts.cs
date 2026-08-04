using UniversalWallet.Api.Payments.Domain.MerchantPayments;

namespace UniversalWallet.Api.Payments.Application.MerchantPayments;

public interface IMerchantProfileRepository
{
    Task<MerchantProfile?> GetAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<MerchantProfile?> GetByAwidAsync(Guid merchantAwid, CancellationToken cancellationToken = default);
    Task AddAsync(MerchantProfile merchant, CancellationToken cancellationToken = default);
    Task UpdateAsync(MerchantProfile merchant, CancellationToken cancellationToken = default);
}

public interface IMerchantPaymentRequestRepository
{
    Task<MerchantPaymentRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<MerchantPaymentRequest?> GetByQrTokenAsync(Guid qrTokenId, CancellationToken cancellationToken = default);
    Task AddAsync(MerchantPaymentRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(MerchantPaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MerchantPaymentRequest>> ListAsync(Guid merchantId, CancellationToken cancellationToken = default);
}

public interface IMerchantQrTokenRepository
{
    Task<MerchantQrToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(MerchantQrToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(MerchantQrToken token, CancellationToken cancellationToken = default);
}

public sealed record ResolveMerchantQrRequest(string Token);
public sealed record ResolveMerchantQrResponse(string QrType, MerchantProfileResponse Merchant, MerchantPaymentRequestResponse? PaymentRequest, string NextAction);
public sealed record MerchantProfileResponse(string DisplayName, string Alias, bool Verified, string Category, string CountryCode);
public sealed record MerchantPaymentRequestResponse(Guid RequestId, long AmountMinor, string CurrencyCode, string Description, DateTimeOffset ExpiresAt);
public sealed record CreateMerchantPaymentRequestRequest(long AmountMinor, string CurrencyCode, string Description, int ExpiresInSeconds, string ExternalReference);
public sealed record CreateMerchantPaymentRequestResponse(Guid RequestId, long AmountMinor, string CurrencyCode, string Description, MerchantPaymentRequestStatus Status);
public sealed record CreateMerchantQrResponse(string Token, MerchantQrType Type);
