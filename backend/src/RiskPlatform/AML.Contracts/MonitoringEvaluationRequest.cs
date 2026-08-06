namespace AML.Contracts;

public sealed class MonitoringEvaluationRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string AWID { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string BeneficiaryId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public int DailyCount { get; set; }
    public int MonthlyCount { get; set; }
    public int DailyAmount { get; set; }
    public int MonthlyAmount { get; set; }
    public int BeneficiaryCount { get; set; }
    public int TransactionFrequency { get; set; }
    public bool NewAccount { get; set; }
    public bool MultiCurrency { get; set; }
    public bool MultiChannel { get; set; }
    public IDictionary<string, object> Profile { get; set; } = new Dictionary<string, object>();
}
