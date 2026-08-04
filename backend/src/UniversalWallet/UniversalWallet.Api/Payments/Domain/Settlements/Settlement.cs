namespace UniversalWallet.Api.Payments.Domain.Settlements;

public enum SettlementStatus
{
    PENDING,
    PROCESSING,
    SETTLED,
    FAILED,
    RETRY_SCHEDULED,
    REVERSED,
    CANCELLED
}

public enum SettlementChannel
{
    INTERNAL,
    MOBILE_MONEY,
    BANK_TRANSFER,
    SEPA,
    CARD,
    MERCHANT
}

public sealed class Settlement
{
    public Guid SettlementId { get; init; } = Guid.CreateVersion7();
    public Guid TransferId { get; init; }
    public Guid PaymentIntentId { get; init; }
    public SettlementChannel Channel { get; set; } = SettlementChannel.INTERNAL;
    public SettlementStatus Status { get; set; } = SettlementStatus.PENDING;
    public string SettlementReference { get; init; } = string.Empty;
    public string? ProviderReference { get; set; }
    public int AttemptCount { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public string CorrelationId { get; init; } = string.Empty;
    public int Version { get; set; } = 1;

    public bool IsTerminal => Status is SettlementStatus.SETTLED or SettlementStatus.REVERSED or SettlementStatus.CANCELLED;
}
