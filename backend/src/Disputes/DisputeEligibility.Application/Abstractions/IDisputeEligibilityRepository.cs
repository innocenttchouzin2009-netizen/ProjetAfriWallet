using AfriWallet.Disputes.Eligibility.Domain.Eligibility;

namespace AfriWallet.Disputes.Eligibility.Application.Abstractions;

public interface IDisputeEligibilityRepository
{
    Task SaveAsync(DisputeEligibilityDecision decision, CancellationToken cancellationToken = default);
    Task<DisputeEligibilityDecision?> GetByClaimAsync(Guid claimId, CancellationToken cancellationToken = default);
}
