namespace MobileMoney.Production.Configuration;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public RetryOptions Retry { get; init; } = new();
    public TimeoutOptions Timeout { get; init; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; init; } = new();

    public sealed class RetryOptions
    {
        public int MaximumAttempts { get; init; } = 3;
        public int BaseDelayMs { get; init; } = 250;
        public bool UseExponentialBackoff { get; init; } = true;
        public bool UseJitter { get; init; } = true;
    }

    public sealed class TimeoutOptions
    {
        public int RequestTimeoutSeconds { get; init; } = 10;
    }

    public sealed class CircuitBreakerOptions
    {
        public double FailureRatio { get; init; } = 0.5;
        public int SamplingDurationSeconds { get; init; } = 30;
        public int MinimumThroughput { get; init; } = 20;
        public int BreakDurationSeconds { get; init; } = 60;
    }
}
