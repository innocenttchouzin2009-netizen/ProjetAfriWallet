namespace Fraud.Domain;

public sealed class FraudCase
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TransactionId { get; init; } = string.Empty;
    public string Decision { get; init; } = "APPROVE";
    public string RiskLevel { get; init; } = "LOW";
    public int Score { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
