using AfriWallet.Fraud.Intelligence.Application.Abstractions;

namespace AfriWallet.Fraud.Intelligence.Infrastructure;

public sealed class SystemFraudIntelligenceClock : IFraudIntelligenceClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}