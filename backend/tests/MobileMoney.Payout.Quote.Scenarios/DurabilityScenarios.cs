using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Domain;
using MobileMoney.Production.Payout.Quote.Infrastructure.Persistence;

internal static class DurabilityScenarios
{
    public static async Task PersistsQuoteAcrossStoreRecreation()
    {
        var databasePath = TemporaryDatabasePath();

        try
        {
            var quote = CreateQuote();

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var store = new EfMobileMoneyPayoutQuoteStore(dbContext);
                await store.SaveAsync(quote);
            }

            SqliteConnection.ClearAllPools();

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var store = new EfMobileMoneyPayoutQuoteStore(dbContext);
                var restored = await store.FindByIdAsync(quote.QuoteId);

                Assert(restored is not null, "Persisted payout quote must survive store recreation.");
                AssertEqual(quote.QuoteId, restored!.QuoteId);
                AssertEqual(quote.Corridor, restored.Corridor);
                AssertEqual(quote.SourceAmountMinor, restored.SourceAmountMinor);
                AssertEqual(quote.DestinationAmountMinor, restored.DestinationAmountMinor);
                AssertEqual(quote.FxRate, restored.FxRate);
                AssertEqual(quote.TotalFeeMinor, restored.TotalFeeMinor);
                AssertEqual(quote.TotalSourceDebitMinor, restored.TotalSourceDebitMinor);
                AssertEqual(quote.CreatedAtUtc, restored.CreatedAtUtc);
                AssertEqual(quote.ExpiresAtUtc, restored.ExpiresAtUtc);
                AssertEqual(quote.Fees.Count, restored.Fees.Count);
                AssertEqual(quote.Fees[0].Code, restored.Fees[0].Code);
                AssertEqual(quote.Fees[0].AmountMinor, restored.Fees[0].AmountMinor);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
        }
    }

    public static async Task PersistsQuoteFundingCorrelationLink()
    {
        var databasePath = TemporaryDatabasePath();

        try
        {
            var quote = CreateQuote();
            var fundingCorrelationId = Guid.NewGuid();

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var store = new EfMobileMoneyPayoutQuoteStore(dbContext);
                await store.SaveAsync(quote);
                await store.LinkFundingCorrelationAsync(
                    quote.QuoteId,
                    fundingCorrelationId);
            }

            SqliteConnection.ClearAllPools();

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var store = new EfMobileMoneyPayoutQuoteStore(dbContext);

                var restoredCorrelation =
                    await store.FindFundingCorrelationIdAsync(quote.QuoteId);
                var restoredQuoteId =
                    await store.FindQuoteIdByFundingCorrelationIdAsync(
                        fundingCorrelationId);

                AssertEqual(fundingCorrelationId, restoredCorrelation);
                AssertEqual(quote.QuoteId, restoredQuoteId);

                await store.LinkFundingCorrelationAsync(
                    quote.QuoteId,
                    fundingCorrelationId);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
        }
    }

    public static async Task RejectsFundingCorrelationRebinding()
    {
        var databasePath = TemporaryDatabasePath();

        try
        {
            var first = CreateQuote();
            var second = CreateQuote();
            var firstCorrelation = Guid.NewGuid();
            var secondCorrelation = Guid.NewGuid();

            await using var dbContext = CreateDbContext(databasePath);
            var store = new EfMobileMoneyPayoutQuoteStore(dbContext);

            await store.SaveAsync(first);
            await store.SaveAsync(second);
            await store.LinkFundingCorrelationAsync(
                first.QuoteId,
                firstCorrelation);

            await AssertThrowsAsync<InvalidOperationException>(() =>
                store.LinkFundingCorrelationAsync(
                    first.QuoteId,
                    secondCorrelation));

            await AssertThrowsAsync<InvalidOperationException>(() =>
                store.LinkFundingCorrelationAsync(
                    second.QuoteId,
                    firstCorrelation));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
        }
    }

    private static MobileMoneyPayoutQuoteDbContext CreateDbContext(
        string databasePath)
    {
        var options =
            new DbContextOptionsBuilder<MobileMoneyPayoutQuoteDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

        return new MobileMoneyPayoutQuoteDbContext(options);
    }

    private static MobileMoneyPayoutQuote CreateQuote()
    {
        var createdAt =
            new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);

        return MobileMoneyPayoutQuote.Create(
            new MobileMoneyPayoutCorridor(
                "DE",
                "EUR",
                "CM",
                "XAF",
                "MTN-CM"),
            10_000,
            6_500_000,
            650m,
            new[]
            {
                new MobileMoneyPayoutFee(
                    "SERVICE_FEE",
                    250,
                    "EUR")
            },
            createdAt,
            createdAt.AddMinutes(10));
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(),
            $"afw-momo-quote-{Guid.NewGuid():N}.db");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Expected '{expected}', got '{actual}'.");
        }
    }

    private static async Task AssertThrowsAsync<TException>(
        Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }
}
