namespace Fraud.Api.Domain;

public enum FraudSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public sealed class FraudRule
{
    public Guid RuleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public FraudSeverity Severity { get; init; }
    public string Condition { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class FraudEvent
{
    public FraudEvent(string ruleName, string walletId, decimal amount, string currency)
    {
        RuleName = ruleName;
        WalletId = walletId;
        Amount = amount;
        Currency = currency;
    }

    public string RuleName { get; }
    public string WalletId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
}

public sealed class FraudEvaluation
{
    public bool IsFlagged { get; init; }
    public string? RuleName { get; init; }
    public FraudSeverity? Severity { get; init; }
}
