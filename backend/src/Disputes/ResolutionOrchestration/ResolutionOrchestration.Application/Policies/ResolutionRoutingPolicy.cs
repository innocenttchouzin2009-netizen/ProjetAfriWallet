using AfriWallet.Disputes.Resolution.Application.Abstractions;
using AfriWallet.Disputes.Resolution.Domain.Resolutions;

namespace AfriWallet.Disputes.Resolution.Application.Policies;

public sealed class ResolutionRoutingPolicy
{
    public ResolutionRoute Resolve(DisputeDecisionSnapshot decision)
    {
        if (!string.Equals(decision.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only approved dispute decisions may be orchestrated.");

        if (string.Equals(decision.DecisionType, "RefundRecommended", StringComparison.OrdinalIgnoreCase))
            return ResolutionRoute.Refund;

        if (string.Equals(decision.DecisionType, "ChargebackRecommended", StringComparison.OrdinalIgnoreCase))
            return ResolutionRoute.Chargeback;

        throw new InvalidOperationException("Decision type cannot be financially orchestrated.");
    }
}
