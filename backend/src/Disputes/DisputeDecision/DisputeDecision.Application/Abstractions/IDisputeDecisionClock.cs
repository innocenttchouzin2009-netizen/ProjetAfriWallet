namespace AfriWallet.Disputes.Decision.Application.Abstractions;

public interface IDisputeDecisionClock
{
    DateTimeOffset UtcNow { get; }
}
