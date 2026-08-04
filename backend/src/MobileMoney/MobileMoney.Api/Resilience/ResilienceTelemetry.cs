namespace MobileMoney.Production.Resilience;

public sealed class ResilienceTelemetry
{
    public int RetryCount { get; private set; }
    public int TimeoutCount { get; private set; }
    public int CircuitBreakerOpened { get; private set; }
    public int CircuitBreakerClosed { get; private set; }
    public int CircuitBreakerHalfOpen { get; private set; }
    public int FallbackTriggered { get; private set; }

    public void RecordRetry() => RetryCount++;
    public void RecordTimeout() => TimeoutCount++;
    public void RecordCircuitOpened() => CircuitBreakerOpened++;
    public void RecordCircuitClosed() => CircuitBreakerClosed++;
    public void RecordCircuitHalfOpen() => CircuitBreakerHalfOpen++;
    public void RecordFallback() => FallbackTriggered++;
}
