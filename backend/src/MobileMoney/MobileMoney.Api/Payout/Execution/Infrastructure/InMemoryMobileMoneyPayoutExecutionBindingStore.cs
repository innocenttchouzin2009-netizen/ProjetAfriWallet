using System.Collections.Concurrent;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Infrastructure;

public sealed class InMemoryMobileMoneyPayoutExecutionBindingStore
    : IMobileMoneyPayoutExecutionBindingStore
{
    private readonly ConcurrentDictionary<string, MobileMoneyPayoutExecutionBinding> _bindings =
        new(StringComparer.Ordinal);

    public Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKey);
        cancellationToken.ThrowIfCancellationRequested();

        _bindings.TryGetValue(idempotencyKey.Value, out var binding);
        return Task.FromResult(binding);
    }

    public Task SaveAsync(
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        cancellationToken.ThrowIfCancellationRequested();

        _bindings.TryAdd(binding.IdempotencyKey.Value, binding);
        return Task.CompletedTask;
    }
}
