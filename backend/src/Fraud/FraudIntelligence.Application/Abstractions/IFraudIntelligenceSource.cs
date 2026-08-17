using AfriWallet.Fraud.Intelligence.Application.Models;

namespace AfriWallet.Fraud.Intelligence.Application.Abstractions;

public interface IFraudIntelligenceSource
{
    Task<IntelligenceSourceSnapshot?> GetAsync(string awid, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IntelligenceSourceSnapshot>> GetNetworkAsync(CancellationToken cancellationToken = default);
}