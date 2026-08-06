namespace Fraud.Contracts;

public sealed class FraudEvaluationRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string AWID { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string GeoLocation { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public int Velocity { get; set; }
    public IDictionary<string, object> HistoricalBehaviour { get; set; } = new Dictionary<string, object>();
}
