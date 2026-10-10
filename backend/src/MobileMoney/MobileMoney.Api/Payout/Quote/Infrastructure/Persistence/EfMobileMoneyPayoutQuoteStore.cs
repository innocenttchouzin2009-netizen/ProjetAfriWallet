using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Infrastructure.Persistence;

public sealed class EfMobileMoneyPayoutQuoteStore(
    MobileMoneyPayoutQuoteDbContext dbContext)
    : IMobileMoneyPayoutQuoteStore
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(
        MobileMoneyPayoutQuote quote,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quote);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Quotes
            .AsNoTracking()
            .AnyAsync(x => x.QuoteId == quote.QuoteId, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Payout quote '{quote.QuoteId}' already exists.");
        }

        dbContext.Quotes.Add(ToEntity(quote));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MobileMoneyPayoutQuote?> FindByIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default)
    {
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id cannot be empty.", nameof(quoteId));

        await EnsureCreatedAsync(cancellationToken);

        var entity = await dbContext.Quotes
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.QuoteId == quoteId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task LinkFundingCorrelationAsync(
        Guid quoteId,
        Guid fundingCorrelationId,
        CancellationToken cancellationToken = default)
    {
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id cannot be empty.", nameof(quoteId));
        if (fundingCorrelationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Funding correlation id cannot be empty.",
                nameof(fundingCorrelationId));
        }

        await EnsureCreatedAsync(cancellationToken);

        var entity = await dbContext.Quotes.SingleOrDefaultAsync(
            x => x.QuoteId == quoteId,
            cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException(
                $"Payout quote '{quoteId}' was not found.");
        }

        if (entity.FundingCorrelationId == fundingCorrelationId)
            return;

        if (entity.FundingCorrelationId is not null)
        {
            throw new InvalidOperationException(
                $"Payout quote '{quoteId}' is already linked to a different funding correlation.");
        }

        var correlationAlreadyLinked = await dbContext.Quotes
            .AsNoTracking()
            .AnyAsync(
                x => x.FundingCorrelationId == fundingCorrelationId,
                cancellationToken);

        if (correlationAlreadyLinked)
        {
            throw new InvalidOperationException(
                $"Funding correlation '{fundingCorrelationId}' is already linked to another payout quote.");
        }

        entity.FundingCorrelationId = fundingCorrelationId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> FindFundingCorrelationIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default)
    {
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id cannot be empty.", nameof(quoteId));

        await EnsureCreatedAsync(cancellationToken);

        return await dbContext.Quotes
            .AsNoTracking()
            .Where(x => x.QuoteId == quoteId)
            .Select(x => x.FundingCorrelationId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> FindQuoteIdByFundingCorrelationIdAsync(
        Guid fundingCorrelationId,
        CancellationToken cancellationToken = default)
    {
        if (fundingCorrelationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Funding correlation id cannot be empty.",
                nameof(fundingCorrelationId));
        }

        await EnsureCreatedAsync(cancellationToken);

        return await dbContext.Quotes
            .AsNoTracking()
            .Where(x => x.FundingCorrelationId == fundingCorrelationId)
            .Select(x => (Guid?)x.QuoteId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Task EnsureCreatedAsync(CancellationToken cancellationToken) =>
        dbContext.Database.EnsureCreatedAsync(cancellationToken);

    private static MobileMoneyPayoutQuoteEntity ToEntity(
        MobileMoneyPayoutQuote quote) =>
        new()
        {
            QuoteId = quote.QuoteId,
            SourceCountryCode = quote.Corridor.SourceCountryCode,
            SourceCurrency = quote.Corridor.SourceCurrency,
            DestinationCountryCode = quote.Corridor.DestinationCountryCode,
            DestinationCurrency = quote.Corridor.DestinationCurrency,
            OperatorCode = quote.Corridor.OperatorCode,
            SourceAmountMinor = quote.SourceAmountMinor,
            DestinationAmountMinor = quote.DestinationAmountMinor,
            FxRate = quote.FxRate,
            FeesJson = JsonSerializer.Serialize(
                quote.Fees.Select(fee => new PersistedFee(
                    fee.Code,
                    fee.AmountMinor,
                    fee.Currency)),
                SerializerOptions),
            TotalFeeMinor = quote.TotalFeeMinor,
            TotalSourceDebitMinor = quote.TotalSourceDebitMinor,
            CreatedAtUtc = quote.CreatedAtUtc,
            ExpiresAtUtc = quote.ExpiresAtUtc
        };

    private static MobileMoneyPayoutQuote ToDomain(
        MobileMoneyPayoutQuoteEntity entity)
    {
        var persistedFees =
            JsonSerializer.Deserialize<PersistedFee[]>(
                entity.FeesJson,
                SerializerOptions)
            ?? throw new InvalidOperationException(
                $"Payout quote '{entity.QuoteId}' contains invalid persisted fees.");

        var fees = persistedFees
            .Select(fee => new MobileMoneyPayoutFee(
                fee.Code,
                fee.AmountMinor,
                fee.Currency))
            .ToArray();

        var expectedFeeMinor = fees.Sum(fee => fee.AmountMinor);
        var expectedTotalDebitMinor =
            checked(entity.SourceAmountMinor + expectedFeeMinor);

        if (entity.TotalFeeMinor != expectedFeeMinor
            || entity.TotalSourceDebitMinor != expectedTotalDebitMinor)
        {
            throw new InvalidOperationException(
                $"Payout quote '{entity.QuoteId}' failed persisted monetary integrity checks.");
        }

        return MobileMoneyPayoutQuote.Restore(
            entity.QuoteId,
            new MobileMoneyPayoutCorridor(
                entity.SourceCountryCode,
                entity.SourceCurrency,
                entity.DestinationCountryCode,
                entity.DestinationCurrency,
                entity.OperatorCode),
            entity.SourceAmountMinor,
            entity.DestinationAmountMinor,
            entity.FxRate,
            fees,
            entity.CreatedAtUtc,
            entity.ExpiresAtUtc);
    }

    private sealed record PersistedFee(
        string Code,
        long AmountMinor,
        string Currency);
}
