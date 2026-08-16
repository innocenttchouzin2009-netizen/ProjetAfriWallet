namespace AfriWallet.Compliance.TransactionMonitoring.Domain.Alerts;

public sealed class MonitoringAlert
{
    public MonitoringAlert(
        Guid alertId,
        Guid transactionId,
        string awid,
        MonitoringSeverity severity,
        int riskScore,
        IReadOnlyCollection<string> ruleCodes,
        DateTimeOffset createdAtUtc)
    {
        AlertId = alertId;
        TransactionId = transactionId;
        Awid = awid;
        Severity = severity;
        RiskScore = riskScore;
        RuleCodes = ruleCodes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid AlertId { get; }
    public Guid TransactionId { get; }
    public string Awid { get; }
    public MonitoringSeverity Severity { get; }
    public int RiskScore { get; }
    public IReadOnlyCollection<string> RuleCodes { get; }
    public MonitoringAlertStatus Status { get; private set; } = MonitoringAlertStatus.Open;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public void StartReview()
    {
        if (Status != MonitoringAlertStatus.Open)
            throw new InvalidOperationException("Only open alerts can enter review.");

        Status = MonitoringAlertStatus.UnderReview;
    }

    public void Escalate()
    {
        if (Status is MonitoringAlertStatus.Closed or MonitoringAlertStatus.FalsePositive)
            throw new InvalidOperationException("Closed alerts cannot be escalated.");

        Status = MonitoringAlertStatus.Escalated;
    }

    public void MarkFalsePositive(DateTimeOffset now)
    {
        Status = MonitoringAlertStatus.FalsePositive;
        ClosedAtUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        Status = MonitoringAlertStatus.Closed;
        ClosedAtUtc = now;
    }
}