using AfriWallet.Disputes.Registry.Application.Abstractions;

namespace AfriWallet.Disputes.Registry.Infrastructure;

public sealed class SystemDisputeRegistryClock : IDisputeRegistryClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
