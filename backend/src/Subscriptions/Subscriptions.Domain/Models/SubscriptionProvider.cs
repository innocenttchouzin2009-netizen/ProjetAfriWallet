namespace Subscriptions.Domain.Models;

public sealed class SubscriptionProvider
{
    public string ProviderId { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SubscriptionCategory Category { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public List<string> SupportedCountries { get; set; } = new();
    public List<string> SupportedCurrencies { get; set; } = new();
    public SubscriptionIntegrationType IntegrationType { get; set; }
    public SubscriptionProviderStatus Status { get; set; } = SubscriptionProviderStatus.ComingSoon;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;
}
