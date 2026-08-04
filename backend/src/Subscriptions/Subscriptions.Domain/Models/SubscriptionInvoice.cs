namespace Subscriptions.Domain.Models;

public enum SubscriptionBillingCycle
{
    OneTime,
    Monthly,
    Quarterly,
    SemiAnnual,
    Annual
}

public enum SubscriptionInvoiceStatus
{
    Draft,
    Pending,
    Paid,
    Failed,
    Overdue,
    Cancelled
}

public enum SubscriptionInvoiceAttemptStatus
{
    Pending,
    Succeeded,
    Failed
}

public sealed class SubscriptionInvoice
{
    public string InvoiceId { get; set; } = Guid.NewGuid().ToString("N");
    public string SubscriptionId { get; set; } = string.Empty;
    public DateTimeOffset BillingPeriodStart { get; set; }
    public DateTimeOffset BillingPeriodEnd { get; set; }
    public string Currency { get; set; } = "XOF";
    public long AmountMinor { get; set; }
    public SubscriptionBillingCycle BillingCycle { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public SubscriptionInvoiceStatus Status { get; set; } = SubscriptionInvoiceStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public List<SubscriptionInvoiceAttempt> Attempts { get; set; } = new();
}

public sealed class SubscriptionInvoiceAttempt
{
    public string AttemptId { get; set; } = Guid.NewGuid().ToString("N");
    public SubscriptionInvoiceAttemptStatus Status { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? GatewayReference { get; set; }
    public string? Error { get; set; }
}
