namespace RiskScoring.Contracts;

public sealed class RiskEvaluationRequest
{
    public string AWID { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public int FraudScore { get; set; }
    public int AmlScore { get; set; }
    public int DeviceScore { get; set; }
    public int AccountAgeDays { get; set; }
    public int BeneficiaryHistoryScore { get; set; }
    public int KycProfileScore { get; set; }
    public int GeoScore { get; set; }
    public int BehaviourScore { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public IDictionary<string, object> Signals { get; set; } = new Dictionary<string, object>();
}
