using AfriWallet.Disputes.Eligibility.Domain.Claims;

namespace AfriWallet.Disputes.Eligibility.Application.Abstractions;

public interface IDisputeClaimReader
{
    Task<DisputeClaimSnapshot?> GetAsync(Guid claimId, CancellationToken cancellationToken = default);
}
