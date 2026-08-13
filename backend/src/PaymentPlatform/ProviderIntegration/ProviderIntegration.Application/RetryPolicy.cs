namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public sealed class RetryPolicy
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldRetry,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(shouldRetry);

        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));

        var retries = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await operation(cancellationToken);

            if (!shouldRetry(result) || retries >= maxRetries)
                return result;

            retries++;

            var delay = TimeSpan.FromMilliseconds(
                Math.Min(2_000, 100 * Math.Pow(2, retries)));

            await Task.Delay(delay, cancellationToken);
        }
    }
}