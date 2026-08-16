using AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

namespace AfriWallet.Compliance.TransactionMonitoring.Infrastructure;

public sealed class SystemMonitoringClock : IMonitoringClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}