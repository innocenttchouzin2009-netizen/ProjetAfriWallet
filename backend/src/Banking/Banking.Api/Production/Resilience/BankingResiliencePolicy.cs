namespace AfriWallet.Banking.Api.Production.Resilience;

public sealed class BankingResiliencePolicy
{
    public bool RetryEnabled { get; init; } = true;
    public int MaxRetries { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 10;
    public bool CircuitBreakerEnabled { get; init; } = true;
    public bool FallbackEnabled { get; init; } = true;
    public bool RecoveryEnabled { get; init; } = true;
}
