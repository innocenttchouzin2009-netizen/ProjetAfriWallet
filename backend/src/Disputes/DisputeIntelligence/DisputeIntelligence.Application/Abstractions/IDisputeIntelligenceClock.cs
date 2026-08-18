namespace AfriWallet.Disputes.Intelligence.Application.Abstractions;

public interface IDisputeIntelligenceClock
{
    DateTimeOffset UtcNow { get; }
}
