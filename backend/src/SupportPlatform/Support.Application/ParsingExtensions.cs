using Support.Domain;

namespace Support.Application;

internal static class ParsingExtensions
{
    public static SupportCaseCategory ParseCategory(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "ACCOUNT" => SupportCaseCategory.Account,
            "WALLET" => SupportCaseCategory.Wallet,
            "PAYMENT" => SupportCaseCategory.Payment,
            "MOBILEMONEY" => SupportCaseCategory.MobileMoney,
            "BANKING" => SupportCaseCategory.Banking,
            "CARD" => SupportCaseCategory.Card,
            "MERCHANT" => SupportCaseCategory.Merchant,
            "SUBSCRIPTION" => SupportCaseCategory.Subscription,
            "SECURITY" => SupportCaseCategory.Security,
            "IDENTITY" => SupportCaseCategory.Identity,
            "DEVELOPERAPI" => SupportCaseCategory.DeveloperApi,
            "COMPLAINT" => SupportCaseCategory.Complaint,
            "DISPUTE" => SupportCaseCategory.Dispute,
            _ => SupportCaseCategory.Other
        };
    }

    public static SupportCasePriority ParsePriority(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "LOW" => SupportCasePriority.Low,
            "NORMAL" => SupportCasePriority.Normal,
            "HIGH" => SupportCasePriority.High,
            "URGENT" => SupportCasePriority.Urgent,
            "CRITICAL" => SupportCasePriority.Critical,
            _ => SupportCasePriority.Normal
        };
    }

    public static SupportCaseStatus ParseStatus(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "OPEN" => SupportCaseStatus.Open,
            "ASSIGNED" => SupportCaseStatus.Assigned,
            "INPROGRESS" => SupportCaseStatus.InProgress,
            "WAITINGFORCUSTOMER" => SupportCaseStatus.WaitingForCustomer,
            "WAITINGFORPARTNER" => SupportCaseStatus.WaitingForPartner,
            "ESCALATED" => SupportCaseStatus.Escalated,
            "RESOLVED" => SupportCaseStatus.Resolved,
            "CLOSED" => SupportCaseStatus.Closed,
            "REOPENED" => SupportCaseStatus.Reopened,
            "CANCELLED" => SupportCaseStatus.Cancelled,
            _ => SupportCaseStatus.Open
        };
    }

    public static SupportCaseChannel ParseChannel(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "MOBILEAPP" => SupportCaseChannel.MobileApp,
            "WEBPORTAL" => SupportCaseChannel.WebPortal,
            "EMAIL" => SupportCaseChannel.Email,
            "PHONE" => SupportCaseChannel.Phone,
            "CHAT" => SupportCaseChannel.Chat,
            "BACKOFFICE" => SupportCaseChannel.BackOffice,
            "API" => SupportCaseChannel.Api,
            "SYSTEMGENERATED" => SupportCaseChannel.SystemGenerated,
            _ => SupportCaseChannel.WebPortal
        };
    }

    public static string ToWire(this SupportCaseCategory category)
    {
        return category switch
        {
            SupportCaseCategory.MobileMoney => "MOBILE_MONEY",
            SupportCaseCategory.DeveloperApi => "DEVELOPER_API",
            _ => category.ToString().ToUpperInvariant()
        };
    }

    public static string ToWire(this SupportCaseStatus status)
    {
        return status switch
        {
            SupportCaseStatus.InProgress => "IN_PROGRESS",
            SupportCaseStatus.WaitingForCustomer => "WAITING_FOR_CUSTOMER",
            SupportCaseStatus.WaitingForPartner => "WAITING_FOR_PARTNER",
            _ => status.ToString().ToUpperInvariant()
        };
    }

    public static string ToWire(this SupportCaseChannel channel)
    {
        return channel switch
        {
            SupportCaseChannel.MobileApp => "MOBILE_APP",
            SupportCaseChannel.WebPortal => "WEB_PORTAL",
            SupportCaseChannel.BackOffice => "BACK_OFFICE",
            SupportCaseChannel.SystemGenerated => "SYSTEM_GENERATED",
            _ => channel.ToString().ToUpperInvariant()
        };
    }

    private static string Normalize(string value)
    {
        return value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }
}
