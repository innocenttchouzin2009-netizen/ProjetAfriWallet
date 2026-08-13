namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Resilience;

public sealed class ProviderCircuitBreaker
{
    private readonly object _sync = new();
    private int _failures;
    private DateTime? _openUntilUtc;

    public bool CanExecute()
    {
        lock (_sync)
        {
            if (_openUntilUtc is null)
                return true;

            if (DateTime.UtcNow >= _openUntilUtc)
            {
                _failures = 0;
                _openUntilUtc = null;
                return true;
            }

            return false;
        }
    }

    public void RecordSuccess()
    {
        lock (_sync)
        {
            _failures = 0;
            _openUntilUtc = null;
        }
    }

    public void RecordFailure(int failureThreshold = 3)
    {
        lock (_sync)
        {
            _failures++;
            if (_failures >= failureThreshold)
            {
                _openUntilUtc = DateTime.UtcNow.AddSeconds(30);
            }
        }
    }
}
