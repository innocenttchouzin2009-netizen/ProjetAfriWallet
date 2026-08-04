using MobileMoney.Production.Configuration;

namespace MobileMoney.Production.Resilience;

public static class TimeoutPolicyFactory
{
    public static TimeSpan BuildRequestTimeout(ResilienceOptions options) =>
        TimeSpan.FromSeconds(options.Timeout.RequestTimeoutSeconds);
}
