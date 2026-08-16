namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Alerts;

public enum MonitoringAlertStatus
{
    Open = 0,
    UnderReview = 1,
    Escalated = 2,
    Closed = 3,
    FalsePositive = 4
}