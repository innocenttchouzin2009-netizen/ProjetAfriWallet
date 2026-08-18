namespace AfriWallet.Disputes.Registry.Application.Abstractions;

public interface IDisputeRegistryClock
{
    DateTimeOffset UtcNow { get; }
}
