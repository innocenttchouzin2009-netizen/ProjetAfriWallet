namespace MobileMoney.Production.RateLimiting;

public static class MobileMoneyRateLimitPolicies
{
    public const string StatusPerIp = "mtn-momo-status-ip";
    public const string OperationsPerAwid = "mtn-momo-operation-awid";
    public const string OperationsPerWallet = "mtn-momo-operation-wallet";
    public const string OperationsPerPhone = "mtn-momo-operation-phone";
    public const string ConnectorConcurrency = "mtn-momo-connector-concurrency";
    public const string Callback = "mtn-momo-callback";
}
