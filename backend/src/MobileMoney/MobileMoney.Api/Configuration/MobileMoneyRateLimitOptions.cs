namespace MobileMoney.Production.Configuration;

public sealed class MobileMoneyRateLimitOptions
{
    public const string SectionName = "MobileMoneyRateLimiting";

    public RateLimitRuleOptions StatusPerIp { get; init; } = new();
    public RateLimitRuleOptions OperationsPerAwid { get; init; } = new();
    public RateLimitRuleOptions OperationsPerWallet { get; init; } = new();
    public RateLimitRuleOptions OperationsPerPhone { get; init; } = new();
    public ConnectorConcurrencyOptions ConnectorConcurrency { get; init; } = new();
    public RateLimitRuleOptions Callback { get; init; } = new();

    public sealed class RateLimitRuleOptions
    {
        public int PermitLimit { get; init; } = 60;
        public int WindowSeconds { get; init; } = 60;
    }

    public sealed class ConnectorConcurrencyOptions
    {
        public int PermitLimit { get; init; } = 100;
        public int QueueLimit { get; init; } = 50;
    }
}
