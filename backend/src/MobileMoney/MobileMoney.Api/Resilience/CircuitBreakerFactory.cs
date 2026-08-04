using MobileMoney.Production.Configuration;

namespace MobileMoney.Production.Resilience;

public sealed class CircuitBreakerFactory
{
    public CircuitBreakerState Build(ResilienceOptions options) => new(options);
}

public sealed class CircuitBreakerState
{
    private readonly ResilienceOptions _options;
    private int _failureCount;
    private DateTimeOffset _openedAt = DateTimeOffset.MinValue;
    private string _state = "Closed";

    public CircuitBreakerState(ResilienceOptions options)
    {
        _options = options;
    }

    public string State => _state;

    public void RecordSuccess()
    {
        _failureCount = 0;
        if (_state != "Open")
        {
            _state = "Closed";
        }
    }

    public void RecordFailure()
    {
        _failureCount++;
        var threshold = (int)Math.Ceiling(_options.CircuitBreaker.MinimumThroughput * _options.CircuitBreaker.FailureRatio);
        if (_state == "Closed" && _failureCount >= threshold)
        {
            _state = "Open";
            _openedAt = DateTimeOffset.UtcNow;
        }
    }

    public bool CanExecute()
    {
        if (_state != "Open")
        {
            return true;
        }

        if (DateTimeOffset.UtcNow - _openedAt < TimeSpan.FromSeconds(_options.CircuitBreaker.BreakDurationSeconds))
        {
            return false;
        }

        _state = "HalfOpen";
        return true;
    }
}
