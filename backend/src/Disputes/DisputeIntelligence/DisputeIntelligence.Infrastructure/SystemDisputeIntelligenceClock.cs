using AfriWallet.Disputes.Intelligence.Application.Abstractions;

namespace AfriWallet.Disputes.Intelligence.Infrastructure;

public sealed class SystemDisputeIntelligenceClock : IDisputeIntelligenceClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
