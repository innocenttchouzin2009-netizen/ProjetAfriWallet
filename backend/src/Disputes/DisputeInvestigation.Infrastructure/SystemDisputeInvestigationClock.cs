using AfriWallet.Disputes.Investigation.Application.Abstractions;

namespace AfriWallet.Disputes.Investigation.Infrastructure;

public sealed class SystemDisputeInvestigationClock : IDisputeInvestigationClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
