namespace Subscriptions.Api.Configuration;

public sealed class MtnMomoOptions
{
    public const string SectionName = "MtnMomo";

    public string Environment { get; set; } = "Development";
    public string BaseUrl { get; set; } = "https://sandbox.example.com";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableSandbox { get; set; } = true;
    public string ProviderName { get; set; } = "MTN";
}
