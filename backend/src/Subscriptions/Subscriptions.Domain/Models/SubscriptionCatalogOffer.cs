namespace Subscriptions.Domain.Models;

public sealed class SubscriptionCatalogOffer
{
    public string OfferId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProviderId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SubscriptionCategory Category { get; set; }
    public string Country { get; set; } = "CM";
    public string Currency { get; set; } = "XOF";
    public long PriceMinor { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public bool IsFeatured { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public string? PromotionCode { get; set; }
    public int? DiscountPercent { get; set; }
    public bool IsNew { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
