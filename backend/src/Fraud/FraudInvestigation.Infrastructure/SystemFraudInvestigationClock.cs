using AfriWallet.Fraud.Investigation.Application.Abstractions;

namespace AfriWallet.Fraud.Investigation.Infrastructure;

public sealed class SystemFraudInvestigationClock : IFraudInvestigationClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}