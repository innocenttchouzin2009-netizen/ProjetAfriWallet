using AfriWallet.Disputes.Decision.Application.Abstractions;

namespace AfriWallet.Disputes.Decision.Infrastructure;

public sealed class SystemDisputeDecisionClock : IDisputeDecisionClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
