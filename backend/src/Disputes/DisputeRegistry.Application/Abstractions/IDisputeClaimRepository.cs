using AfriWallet.Disputes.Registry.Domain.Claims;

namespace AfriWallet.Disputes.Registry.Application.Abstractions;

public interface IDisputeClaimRepository
{
    Task AddAsync(DisputeClaim claim, CancellationToken cancellationToken = default);
    Task SaveAsync(DisputeClaim claim, CancellationToken cancellationToken = default);
    Task<DisputeClaim?> GetAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisputeClaim>> GetByAwidAsync(string awid, CancellationToken cancellationToken = default);
}
