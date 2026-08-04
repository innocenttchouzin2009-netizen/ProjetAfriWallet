namespace Subscriptions.Domain.Models;

public enum SubscriptionConnectorResultStatus
{
    Success,
    Conflict,
    Failed
}

public enum SubscriptionConnectorHealthStatus
{
    Healthy,
    Unhealthy
}

public sealed class SubscriptionProviderConnectorCapabilities
{
    public bool SupportsActivation { get; set; }
    public bool SupportsRenewal { get; set; }
    public bool SupportsSuspension { get; set; }
    public bool SupportsResumption { get; set; }
    public bool SupportsCancellation { get; set; }
    public bool SupportsStatusLookup { get; set; }
}

public sealed class SubscriptionProviderConnectorResponse
{
    public string ProviderId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public SubscriptionConnectorResultStatus Status { get; set; }
    public string? CorrelationId { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string> Payload { get; set; } = new();
}

public sealed class SubscriptionProviderConnectorHealth
{
    public string ProviderId { get; set; } = string.Empty;
    public SubscriptionConnectorHealthStatus Status { get; set; }
    public string? Message { get; set; }
}
