using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;
using MobileMoney.Production.Payout.Funding.Domain;

internal static class DurabilityScenarios
{
    public static async Task PersistsBindingAcrossStoreRecreation()
    {
        var databasePath = TemporaryDatabasePath();

        try
        {
            var binding = CreateBinding("momo-execution-durable-1");

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var store =
                    new EfMobileMoneyPayoutExecutionBindingStore(dbContext);
                await store.SaveAsync(binding);
            }

            SqliteConnection.ClearAllPools();

            await using (var dbContext = CreateDbContext(databasePath))
            {
                var store =
                    new EfMobileMoneyPayoutExecutionBindingStore(dbContext);
                var restored = await store.FindByIdempotencyKeyAsync(
                    binding.IdempotencyKey);

                Assert(
                    restored is not null,
                    "Persisted payout execution binding must survive store recreation.");

                AssertEqual(binding.BindingId, restored!.BindingId);
                AssertEqual(binding.IdempotencyKey.Value, restored.IdempotencyKey.Value);
                AssertEqual(binding.CreatedAtUtc, restored.CreatedAtUtc);

                AssertEqual(binding.Quote.QuoteId, restored.Quote.QuoteId);
                AssertEqual(binding.Quote.SourceCurrency, restored.Quote.SourceCurrency);
                AssertEqual(binding.Quote.SourceAmountMinor, restored.Quote.SourceAmountMinor);
                AssertEqual(binding.Quote.TotalFeeMinor, restored.Quote.TotalFeeMinor);
                AssertEqual(binding.Quote.TotalSourceDebitMinor, restored.Quote.TotalSourceDebitMinor);
                AssertEqual(binding.Quote.DestinationCurrency, restored.Quote.DestinationCurrency);
                AssertEqual(binding.Quote.DestinationAmountMinor, restored.Quote.DestinationAmountMinor);
                AssertEqual(binding.Quote.ExpiresAtUtc, restored.Quote.ExpiresAtUtc);

                AssertEqual(binding.Funding.CorrelationId, restored.Funding.CorrelationId);
                AssertEqual(binding.Funding.RequiredAmountMinor, restored.Funding.RequiredAmountMinor);
                AssertEqual(binding.Funding.CurrencyCode, restored.Funding.CurrencyCode);
                AssertEqual(binding.Funding.Allocations.Count, restored.Funding.Allocations.Count);

                for (var index = 0; index < binding.Funding.Allocations.Count; index++)
                {
                    var expected = binding.Funding.Allocations[index];
                    var actual = restored.Funding.Allocations[index];

                    AssertEqual(expected.SourceId, actual.SourceId);
                    AssertEqual(expected.SourceType, actual.SourceType);
                    AssertEqual(expected.AmountMinor, actual.AmountMinor);
                    AssertEqual(expected.CurrencyCode, actual.CurrencyCode);
                }
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
        }
    }

    public static async Task RepeatedSaveOfSameBindingIsIdempotent()
    {
        var databasePath = TemporaryDatabasePath();

        try
        {
            var binding = CreateBinding("momo-execution-repeat-1");

            await using var dbContext = CreateDbContext(databasePath);
            var store =
                new EfMobileMoneyPayoutExecutionBindingStore(dbContext);

            await store.SaveAsync(binding);
            await store.SaveAsync(binding);

            var restored = await store.FindByIdempotencyKeyAsync(
                binding.IdempotencyKey);

            Assert(restored is not null, "Repeated save must retain the binding.");
            AssertEqual(binding.BindingId, restored!.BindingId);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
        }
    }

    public static async Task RejectsConflictingIdempotencyKey()
    {
        var databasePath = TemporaryDatabasePath();

        try
        {
            const string idempotencyKey = "momo-execution-conflict-1";
            var first = CreateBinding(idempotencyKey);
            var conflicting = CreateBinding(idempotencyKey);

            await using var dbContext = CreateDbContext(databasePath);
            var store =
                new EfMobileMoneyPayoutExecutionBindingStore(dbContext);

            await store.SaveAsync(first);

            await AssertThrowsAsync<InvalidOperationException>(
                () => store.SaveAsync(conflicting));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
        }
    }

    private static MobileMoneyPayoutExecutionDbContext CreateDbContext(
        string databasePath)
    {
        var options =
            new DbContextOptionsBuilder<MobileMoneyPayoutExecutionDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

        return new MobileMoneyPayoutExecutionDbContext(options);
    }

    private static MobileMoneyPayoutExecutionBinding CreateBinding(
        string idempotencyKey)
    {
        var createdAt =
            new DateTimeOffset(2026, 10, 12, 6, 30, 0, TimeSpan.Zero);

        var quote = MobileMoneyPayoutExecutionQuoteBinding.Create(
            Guid.NewGuid(),
            "EUR",
            10_000,
            250,
            10_250,
            "XAF",
            6_500_000,
            createdAt.AddMinutes(10));

        var funding = MobileMoneyPayoutExecutionFundingBinding.Create(
            Guid.NewGuid(),
            10_250,
            "EUR",
            new[]
            {
                MobileMoneyPayoutExecutionFundingAllocation.Create(
                    "wallet-primary",
                    FundingSourceType.Wallet,
                    8_000,
                    "EUR"),
                MobileMoneyPayoutExecutionFundingAllocation.Create(
                    "applepay-fallback",
                    FundingSourceType.ApplePay,
                    2_250,
                    "EUR")
            });

        return MobileMoneyPayoutExecutionBinding.Create(
            quote,
            funding,
            MobileMoneyPayoutExecutionIdempotencyKey.Create(idempotencyKey),
            createdAt);
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(),
            $"afw-momo-execution-{Guid.NewGuid():N}.db");

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
