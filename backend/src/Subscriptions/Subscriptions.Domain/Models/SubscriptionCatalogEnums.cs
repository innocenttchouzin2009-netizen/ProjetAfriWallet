namespace Subscriptions.Domain.Models;

public enum SubscriptionCategory
{
    VideoStreaming,
    Tv,
    Music,
    Sport,
    News,
    Software,
    Cloud,
    Education,
    Gaming,
    Internet,
    Telecom,
    Utility,
    Other
}

public enum SubscriptionIntegrationType
{
    DirectApi,
    Voucher,
    Redirect,
    Manual,
    Partner
}

public enum SubscriptionProviderStatus
{
    Active,
    ComingSoon,
    Suspended,
    Deprecated
}

public enum SubscriptionPlanStatus
{
    Active,
    ComingSoon,
    Suspended,
    Deprecated
}
