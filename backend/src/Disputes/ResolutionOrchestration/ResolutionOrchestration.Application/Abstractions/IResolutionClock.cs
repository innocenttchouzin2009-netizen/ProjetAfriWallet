namespace AfriWallet.Disputes.Resolution.Application.Abstractions;

public interface IResolutionClock
{
    DateTimeOffset UtcNow { get; }
}
