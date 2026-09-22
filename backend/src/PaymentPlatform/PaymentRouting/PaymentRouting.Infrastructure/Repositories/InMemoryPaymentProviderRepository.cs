using System.Collections.Concurrent;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Domain.Providers;

namespace PaymentRouting.Infrastructure.Repositories;

public sealed class InMemoryPaymentProviderRepository :
    IPaymentProviderRepository
{
    private readonly ConcurrentDictionary<
        string,
        PaymentProvider> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(
        PaymentProvider provider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_providers.TryAdd(
                provider.ProviderId,
                provider))
        {
            throw new InvalidOperationException(
                "Payment provider already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<PaymentProvider?> GetAsync(
        string providerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _providers.TryGetValue(
            providerId,
            out var provider);

        return Task.FromResult(provider);
    }

    public Task UpdateAsync(
        PaymentProvider provider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_providers.ContainsKey(provider.ProviderId))
            throw new KeyNotFoundException("Payment provider not found.");

        _providers[provider.ProviderId] = provider;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<PaymentProvider>>
        ListAsync(
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<
            IReadOnlyCollection<PaymentProvider>>(
            _providers.Values.ToArray());
    }
}
