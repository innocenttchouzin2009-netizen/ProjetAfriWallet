using AfriWallet.Merchants.Intelligence.Application.Models;
namespace AfriWallet.Merchants.Intelligence.Application.Abstractions;
public interface IMerchantIntelligenceSource { Task<MerchantIntelligenceSnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default); }
