using MobileMoney.Production.Configuration;

namespace MobileMoney.Production.Resilience;

public static class RetryPolicyFactory
{
    public static int BuildMaximumAttempts(ResilienceOptions options) => options.Retry.MaximumAttempts;

    public static TimeSpan BuildDelay(int attempt, ResilienceOptions options)
    {
        var delay = options.Retry.BaseDelayMs * Math.Pow(2, attempt - 1);
        if (!options.Retry.UseExponentialBackoff)
        {
            delay = options.Retry.BaseDelayMs;
        }

        if (options.Retry.UseJitter)
        {
            delay += (int)(Random.Shared.NextDouble() * 50);
        }

        return TimeSpan.FromMilliseconds(delay);
    }
}
