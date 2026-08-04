namespace UniversalWallet.Api.Fraud.Domain;

public enum FraudDecision
{
    Allow,
    StepUp,
    Review,
    Block
}

public enum FraudRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum FraudReviewStatus
{
    Open,
    InReview,
    Approved,
    Declined,
    Escalated,
    Closed
}

public sealed class FraudAssessment
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PaymentIntentId { get; init; }
    public Guid PayerAwidId { get; init; }
    public Guid SourceWalletId { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public int RiskScore { get; init; }
    public FraudRiskLevel RiskLevel { get; init; }
    public FraudDecision Decision { get; init; }
    public string RuleSetVersion { get; init; } = string.Empty;
    public IReadOnlyList<string> TriggeredRules { get; init; } = Array.Empty<string>();
    public string RecommendedAction { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; init; } = DateTimeOffset.UtcNow.AddMinutes(15);
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed class FraudReviewCase
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AssessmentId { get; init; }
    public Guid PaymentIntentId { get; init; }
    public FraudReviewStatus Status { get; init; } = FraudReviewStatus.Open;
    public string Priority { get; init; } = "MEDIUM";
    public string AssignedTo { get; init; } = string.Empty;
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; init; }
    public FraudDecision? Decision { get; init; }
    public string Notes { get; init; } = string.Empty;
}
