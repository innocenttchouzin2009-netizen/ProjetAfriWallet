namespace AfriWallet.Fraud.Decision.Application.Abstractions;

public interface IFraudDecisionClock
{
    DateTimeOffset UtcNow { get; }
}