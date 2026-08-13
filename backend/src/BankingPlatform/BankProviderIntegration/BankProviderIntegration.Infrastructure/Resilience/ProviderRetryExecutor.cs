namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Resilience;

public sealed class ProviderRetryExecutor
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldRetry,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        if (maxAttempts is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        T? last = default;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                last = await operation(cancellationToken);
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(100 * attempt),
                    cancellationToken);
                continue;
            }

            if (!shouldRetry(last!))
                return last!;

            if (attempt < maxAttempts)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(100 * attempt),
                    cancellationToken);
            }
        }

        return last!;
    }
}
