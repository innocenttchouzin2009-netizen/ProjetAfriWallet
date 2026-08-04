namespace MobileMoney.Production.Configuration;

public sealed class MtnMomoProductionOptions
{
    public const string SectionName = "MtnMomo";

    public string Environment { get; set; } = "Development";
    public bool EnableProduction { get; set; }
    public string BaseUrl { get; set; } = "https://sandbox.example.com";
    public string ApiUserSecretName { get; set; } = "MTN_MOMO_API_USER";
    public string ApiKeySecretName { get; set; } = "MTN_MOMO_API_KEY";
    public string SubscriptionKeySecretName { get; set; } = "MTN_MOMO_SUBSCRIPTION_KEY";
    public string CallbackSecretName { get; set; } = "MTN_MOMO_CALLBACK_SECRET";
    public int TimeoutSeconds { get; set; } = 30;
}
