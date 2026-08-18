namespace AfriWallet.Disputes.Eligibility.Application.Abstractions;

public interface IDisputeEligibilityClock
{
    DateTimeOffset UtcNow { get; }
}
