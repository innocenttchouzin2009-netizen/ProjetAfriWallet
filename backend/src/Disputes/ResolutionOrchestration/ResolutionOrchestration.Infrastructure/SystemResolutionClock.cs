using AfriWallet.Disputes.Resolution.Application.Abstractions;

namespace AfriWallet.Disputes.Resolution.Infrastructure;

public sealed class SystemResolutionClock : IResolutionClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
