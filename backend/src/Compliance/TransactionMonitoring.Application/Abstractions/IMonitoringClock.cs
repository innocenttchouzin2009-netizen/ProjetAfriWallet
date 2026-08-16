namespace AfriWallet.Compliance.TransactionMonitoring.Application.Abstractions;

public interface IMonitoringClock
{
    DateTimeOffset UtcNow { get; }
}