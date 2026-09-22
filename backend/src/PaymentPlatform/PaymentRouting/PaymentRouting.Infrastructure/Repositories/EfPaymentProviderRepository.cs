using Microsoft.EntityFrameworkCore;
using PaymentRouting.Application.Interfaces;
using PaymentRouting.Domain.Providers;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Persistence;

namespace PaymentRouting.Infrastructure.Repositories;

public sealed class EfPaymentProviderRepository(PaymentRoutingDbContext dbContext)
    : IPaymentProviderRepository
{
    public async Task AddAsync(PaymentProvider provider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (await dbContext.Providers.AnyAsync(x => x.ProviderId == provider.ProviderId, cancellationToken))
            throw new InvalidOperationException("Payment provider already exists.");

        dbContext.Providers.Add(ToEntity(provider));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentProvider?> GetAsync(string providerId, CancellationToken cancellationToken)
    {
        var normalized = providerId.Trim();
        var entity = await dbContext.Providers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProviderId == normalized, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task UpdateAsync(PaymentProvider provider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var entity = await dbContext.Providers
            .SingleOrDefaultAsync(x => x.ProviderId == provider.ProviderId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment provider not found.");

        entity.DisplayName = provider.DisplayName;
        entity.Rail = (int)provider.Rail;
        entity.CountriesCsv = string.Join(',', provider.Countries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        entity.CurrenciesCsv = string.Join(',', provider.Currencies.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        entity.BaseCostScore = provider.BaseCostScore;
        entity.Priority = provider.Priority;
        entity.Status = (int)provider.Status;
        entity.SuccessRate = provider.SuccessRate;
        entity.AverageLatencyMs = provider.AverageLatencyMs;
        entity.UpdatedAtUtc = DateTime.SpecifyKind(provider.UpdatedAtUtc, DateTimeKind.Utc);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentProvider>> ListAsync(CancellationToken cancellationToken) =>
        (await dbContext.Providers.AsNoTracking()
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.ProviderId)
            .ToListAsync(cancellationToken))
        .Select(ToDomain)
        .ToArray();

    private static PaymentProviderEntity ToEntity(PaymentProvider value) => new()
    {
        ProviderId = value.ProviderId,
        DisplayName = value.DisplayName,
        Rail = (int)value.Rail,
        CountriesCsv = string.Join(',', value.Countries.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
        CurrenciesCsv = string.Join(',', value.Currencies.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
        BaseCostScore = value.BaseCostScore,
        Priority = value.Priority,
        Status = (int)value.Status,
        SuccessRate = value.SuccessRate,
        AverageLatencyMs = value.AverageLatencyMs,
        UpdatedAtUtc = DateTime.SpecifyKind(value.UpdatedAtUtc, DateTimeKind.Utc)
    };

    private static PaymentProvider ToDomain(PaymentProviderEntity value) =>
        PaymentProvider.Restore(
            value.ProviderId,
            value.DisplayName,
            (PaymentRail)value.Rail,
            value.CountriesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            value.CurrenciesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            value.BaseCostScore,
            value.Priority,
            (ProviderStatus)value.Status,
            value.SuccessRate,
            value.AverageLatencyMs,
            DateTime.SpecifyKind(value.UpdatedAtUtc, DateTimeKind.Utc));
}
