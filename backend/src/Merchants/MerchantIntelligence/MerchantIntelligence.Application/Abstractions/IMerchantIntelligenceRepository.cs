using AfriWallet.Merchants.Intelligence.Domain.Findings;
namespace AfriWallet.Merchants.Intelligence.Application.Abstractions;
public interface IMerchantIntelligenceRepository { Task SaveAsync(MerchantRiskFinding finding, CancellationToken cancellationToken = default); Task<MerchantRiskFinding?> GetLatestAsync(string merchantId, CancellationToken cancellationToken = default); }
