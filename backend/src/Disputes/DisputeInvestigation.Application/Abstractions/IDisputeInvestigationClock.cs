namespace AfriWallet.Disputes.Investigation.Application.Abstractions;

public interface IDisputeInvestigationClock
{
    DateTimeOffset UtcNow { get; }
}
