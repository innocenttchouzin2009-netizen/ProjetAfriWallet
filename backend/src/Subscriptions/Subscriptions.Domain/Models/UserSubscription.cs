namespace Subscriptions.Domain.Models;

public enum UserSubscriptionStatus
{
    Draft,
    PendingPayment,
    Active,
    Suspended,
    GracePeriod,
    Cancelled,
    Expired
}

public sealed class UserSubscription
{
    public string SubscriptionId { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public string Currency { get; set; } = "XOF";
    public long AmountMinor { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public int GracePeriodDays { get; set; }
    public UserSubscriptionStatus Status { get; set; } = UserSubscriptionStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset? RenewalAt { get; set; }
    public DateTimeOffset? LastPaymentAt { get; set; }
    public List<UserSubscriptionStatusChange> History { get; set; } = new();
}

public sealed class UserSubscriptionStatusChange
{
    public UserSubscriptionStatus Status { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}
