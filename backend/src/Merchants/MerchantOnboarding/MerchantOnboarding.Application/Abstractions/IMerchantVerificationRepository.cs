using AfriWallet.Merchants.Onboarding.Domain.Cases;

namespace AfriWallet.Merchants.Onboarding.Application.Abstractions;

public interface IMerchantVerificationRepository
{
    Task AddAsync(MerchantVerificationCase verification, CancellationToken cancellationToken = default);
    Task SaveAsync(MerchantVerificationCase verification, CancellationToken cancellationToken = default);
    Task<MerchantVerificationCase?> GetAsync(Guid verificationId, CancellationToken cancellationToken = default);
    Task<MerchantVerificationCase?> GetByMerchantAsync(string merchantId, CancellationToken cancellationToken = default);
}
