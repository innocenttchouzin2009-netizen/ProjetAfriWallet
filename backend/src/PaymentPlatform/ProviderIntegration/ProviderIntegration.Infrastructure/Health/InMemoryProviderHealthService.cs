using System.Collections.Concurrent;
using AfriWallet.PaymentPlatform.ProviderIntegration.Application;
using AfriWallet.PaymentPlatform.ProviderIntegration.Domain;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Infrastructure.Health;

public sealed class InMemoryProviderHealthService : IProviderHealthService
{
    private sealed class State
    {
        public object Sync { get; } = new();

        public long Successes { get; set; }

        public long Failures { get; set; }

        public double TotalLatency { get; set; }
    }

    private readonly ConcurrentDictionary<string, State> _states =
        new(StringComparer.OrdinalIgnoreCase);

    public ProviderHealth Get(string providerCode)
    {
        var state = GetState(providerCode);

        lock (state.Sync)
        {
            var total = state.Successes + state.Failures;
            var successRate = total == 0 ? 1d : (double)state.Successes / total;
            var latency = total == 0 ? 0 : state.TotalLatency / total;

            return new ProviderHealth(
                providerCode,
                successRate >= 0.5,
                successRate,
                latency,
                DateTimeOffset.UtcNow);
        }
    }

    public void RecordSuccess(string providerCode, double latencyMs)
    {
        ValidateLatency(latencyMs);
        var state = GetState(providerCode);

        lock (state.Sync)
        {
            state.Successes++;
            state.TotalLatency += latencyMs;
        }
    }

    public void RecordFailure(string providerCode, double latencyMs)
    {
        ValidateLatency(latencyMs);
        var state = GetState(providerCode);

        lock (state.Sync)
        {
            state.Failures++;
            state.TotalLatency += latencyMs;
        }
    }

    private State GetState(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new ArgumentException("Provider code is required.", nameof(providerCode));

        return _states.GetOrAdd(providerCode, static _ => new State());
    }

    private static void ValidateLatency(double latencyMs)
    {
        if (!double.IsFinite(latencyMs) || latencyMs < 0)
            throw new ArgumentOutOfRangeException(nameof(latencyMs));
    }
}