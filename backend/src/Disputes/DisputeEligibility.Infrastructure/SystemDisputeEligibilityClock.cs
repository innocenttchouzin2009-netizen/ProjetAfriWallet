using AfriWallet.Disputes.Eligibility.Application.Abstractions;

namespace AfriWallet.Disputes.Eligibility.Infrastructure;

public sealed class SystemDisputeEligibilityClock : IDisputeEligibilityClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
