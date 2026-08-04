namespace Subscriptions.Domain.Models;

public sealed class SubscriptionPlan
{
    public string PlanId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProviderId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "monthly";
    public string Currency { get; set; } = "XOF";
    public long AmountMinor { get; set; }
    public string Country { get; set; } = "CM";
    public SubscriptionPlanStatus Status { get; set; } = SubscriptionPlanStatus.Active;
}
