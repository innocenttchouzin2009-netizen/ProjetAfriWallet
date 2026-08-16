using AfriWallet.Compliance.RiskScoring.Application.Abstractions;

namespace AfriWallet.Compliance.RiskScoring.Infrastructure;

public sealed class SystemRiskClock : IRiskClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}