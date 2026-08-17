using AfriWallet.Fraud.Decision.Application.Abstractions;

namespace AfriWallet.Fraud.Decision.Infrastructure;

public sealed class SystemFraudDecisionClock : IFraudDecisionClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}