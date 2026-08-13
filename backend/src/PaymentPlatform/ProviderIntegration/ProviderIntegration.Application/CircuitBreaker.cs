namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public sealed class CircuitBreaker
{
    private readonly object _sync = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    private int _failures;
    private DateTimeOffset? _openedAt;

    public CircuitBreaker(
        int failureThreshold = 5,
        TimeSpan? openDuration = null)
    {
        if (failureThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(failureThreshold));

        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);

        if (_openDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(openDuration));
    }

    public bool IsOpen
    {
        get
        {
            lock (_sync)
            {
                if (_openedAt is null)
                    return false;

                if (DateTimeOffset.UtcNow - _openedAt >= _openDuration)
                {
                    ResetUnsafe();
                    return false;
                }

                return true;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_sync)
        {
            ResetUnsafe();
        }
    }

    public void RecordFailure()
    {
        lock (_sync)
        {
            _failures++;

            if (_failures >= _failureThreshold && _openedAt is null)
                _openedAt = DateTimeOffset.UtcNow;
        }
    }

    private void ResetUnsafe()
    {
        _failures = 0;
        _openedAt = null;
    }
}