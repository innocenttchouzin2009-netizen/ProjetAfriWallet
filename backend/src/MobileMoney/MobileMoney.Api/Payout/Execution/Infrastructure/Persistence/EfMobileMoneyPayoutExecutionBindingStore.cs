using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;

public sealed class EfMobileMoneyPayoutExecutionBindingStore(
    MobileMoneyPayoutExecutionDbContext dbContext)
    : IMobileMoneyPayoutExecutionBindingStore
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKey);
        await EnsureCreatedAsync(cancellationToken);

        var entity = await dbContext.Bindings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey.Value,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public Task SaveAsync(
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default) =>
        SaveCoreAsync(null, binding, cancellationToken);

    public Task SaveOwnedAsync(
        Guid ownerUserId,
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        return SaveCoreAsync(ownerUserId, binding, cancellationToken);
    }

    private async Task SaveCoreAsync(
        Guid? ownerUserId,
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        await EnsureCreatedAsync(cancellationToken);

        var existing = await dbContext.Bindings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IdempotencyKey == binding.IdempotencyKey.Value,
                cancellationToken);

        if (existing is not null)
            return;

        dbContext.Bindings.Add(ToEntity(ownerUserId, binding));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            var authoritative = await dbContext.Bindings
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.IdempotencyKey == binding.IdempotencyKey.Value,
                    cancellationToken);

            if (authoritative is not null)
                return;

            throw;
        }
    }

    public async Task<Guid?> FindOwnerUserIdAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKey);
        await EnsureCreatedAsync(cancellationToken);

        var owner = await dbContext.Bindings
            .AsNoTracking()
            .Where(x => x.IdempotencyKey == idempotencyKey.Value)
            .Select(x => x.OwnerUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (owner == Guid.Empty)
            throw new InvalidOperationException("Persisted execution binding owner is invalid.");

        return owner;
    }

    private static MobileMoneyPayoutExecutionBindingEntity ToEntity(
        Guid? ownerUserId,
        MobileMoneyPayoutExecutionBinding binding) =>
        new()
        {
            BindingId = binding.BindingId,
            OwnerUserId = ownerUserId,
            IdempotencyKey = binding.IdempotencyKey.Value,
            QuoteId = binding.Quote.QuoteId,
            QuoteSourceCurrency = binding.Quote.SourceCurrency,
            QuoteSourceAmountMinor = binding.Quote.SourceAmountMinor,
            QuoteTotalFeeMinor = binding.Quote.TotalFeeMinor,
            QuoteTotalSourceDebitMinor = binding.Quote.TotalSourceDebitMinor,
            QuoteDestinationCurrency = binding.Quote.DestinationCurrency,
            QuoteDestinationAmountMinor = binding.Quote.DestinationAmountMinor,
            QuoteExpiresAtUtc = binding.Quote.ExpiresAtUtc,
            FundingCorrelationId = binding.Funding.CorrelationId,
            FundingRequiredAmountMinor = binding.Funding.RequiredAmountMinor,
            FundingCurrencyCode = binding.Funding.CurrencyCode,
            FundingAllocationsJson = JsonSerializer.Serialize(
                binding.Funding.Allocations.Select(allocation =>
                    new PersistedFundingAllocation(
                        allocation.SourceId,
                        (int)allocation.SourceType,
                        allocation.AmountMinor,
                        allocation.CurrencyCode)),
                SerializerOptions),
            CreatedAtUtc = binding.CreatedAtUtc
        };

    private static MobileMoneyPayoutExecutionBinding ToDomain(
        MobileMoneyPayoutExecutionBindingEntity entity)
    {
        var persistedAllocations =
            JsonSerializer.Deserialize<PersistedFundingAllocation[]>(
                entity.FundingAllocationsJson,
                SerializerOptions)
            ?? throw new InvalidOperationException(
                "Persisted execution funding allocations are invalid.");

        if (persistedAllocations.Length == 0)
        {
            throw new InvalidOperationException(
                "Persisted execution binding has no funding allocations.");
        }

        var allocations = persistedAllocations.Select(allocation =>
        {
            if (!Enum.IsDefined(typeof(FundingSourceType), allocation.SourceType))
            {
                throw new InvalidOperationException(
                    "Persisted execution binding contains an invalid funding source type.");
            }

            return MobileMoneyPayoutExecutionFundingAllocation.Create(
                allocation.SourceId,
                (FundingSourceType)allocation.SourceType,
                allocation.AmountMinor,
                allocation.CurrencyCode);
        }).ToArray();

        var quote = MobileMoneyPayoutExecutionQuoteBinding.Create(
            entity.QuoteId,
            entity.QuoteSourceCurrency,
            entity.QuoteSourceAmountMinor,
            entity.QuoteTotalFeeMinor,
            entity.QuoteTotalSourceDebitMinor,
            entity.QuoteDestinationCurrency,
            entity.QuoteDestinationAmountMinor,
            entity.QuoteExpiresAtUtc);

        var funding = MobileMoneyPayoutExecutionFundingBinding.Create(
            entity.FundingCorrelationId,
            entity.FundingRequiredAmountMinor,
            entity.FundingCurrencyCode,
            allocations);

        return MobileMoneyPayoutExecutionBinding.Restore(
            entity.BindingId,
            quote,
            funding,
            MobileMoneyPayoutExecutionIdempotencyKey.Create(
                entity.IdempotencyKey),
            entity.CreatedAtUtc);
    }

    private Task EnsureCreatedAsync(CancellationToken cancellationToken) =>
        dbContext.Database.EnsureCreatedAsync(cancellationToken);

    private sealed record PersistedFundingAllocation(
        string SourceId,
        int SourceType,
        long AmountMinor,
        string CurrencyCode);
}
