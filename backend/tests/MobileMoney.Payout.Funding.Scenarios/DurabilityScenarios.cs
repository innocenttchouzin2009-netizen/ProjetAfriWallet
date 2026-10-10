using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Funding.Domain;
using MobileMoney.Production.Payout.Funding.Persistence;

internal static class DurabilityScenarios
{
    public static async Task RestartRoundTrip()
    {
        var databasePath = CreateDatabasePath();

        try
        {
            await CreateDatabaseAsync(databasePath);

            var correlationId = Guid.NewGuid();
            var createdAt =
                new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);

            var first = FundingAttempt.Create(
                Guid.NewGuid(),
                correlationId,
                FundingAllocation.Create(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    4_000,
                    "eur"),
                createdAt);

            var second = FundingAttempt.Create(
                Guid.NewGuid(),
                correlationId,
                FundingAllocation.Create(
                    "apple-pay",
                    FundingSourceType.ApplePay,
                    6_000,
                    "EUR"),
                createdAt.AddSeconds(1));

            await using (var db = CreateContext(databasePath))
            {
                var store =
                    new EfMobileMoneyPayoutFundingAttemptStore(db);
                await store.SaveAsync(first);
                await store.SaveAsync(second);
            }

            await using (var restarted = CreateContext(databasePath))
            {
                var store =
                    new EfMobileMoneyPayoutFundingAttemptStore(restarted);
                var restored =
                    await store.FindByCorrelationIdAsync(correlationId);

                Assert(
                    restored.Count == 2,
                    "Split funding attempts must survive a store restart.");

                Assert(
                    restored[0].Id == first.Id &&
                    restored[1].Id == second.Id,
                    "Restarted store must preserve deterministic attempt ordering.");

                Assert(
                    restored[0].Allocation.SourceType ==
                        FundingSourceType.Wallet &&
                    restored[0].Allocation.AmountMinor == 4_000 &&
                    restored[0].Allocation.CurrencyCode == "EUR",
                    "Wallet funding allocation must round-trip durably.");

                Assert(
                    restored[1].Allocation.SourceType ==
                        FundingSourceType.ApplePay &&
                    restored[1].Allocation.AmountMinor == 6_000,
                    "External funding allocation must round-trip durably.");

                Assert(
                    restored.All(x =>
                        x.Status == FundingAttemptStatus.Planned &&
                        x.StatusReason is null),
                    "Planned funding state must survive restart unchanged.");
            }
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    public static async Task LifecycleRoundTrip()
    {
        var databasePath = CreateDatabasePath();

        try
        {
            await CreateDatabaseAsync(databasePath);

            var correlationId = Guid.NewGuid();
            var createdAt =
                new DateTimeOffset(2026, 10, 10, 2, 30, 0, TimeSpan.Zero);

            var attempt = FundingAttempt.Create(
                Guid.NewGuid(),
                correlationId,
                FundingAllocation.Create(
                    "card-main",
                    FundingSourceType.PaymentCard,
                    2_500,
                    "EUR"),
                createdAt);

            await using (var db = CreateContext(databasePath))
            {
                await new EfMobileMoneyPayoutFundingAttemptStore(db)
                    .SaveAsync(attempt);
            }

            await using (var firstRestart = CreateContext(databasePath))
            {
                var store =
                    new EfMobileMoneyPayoutFundingAttemptStore(firstRestart);
                var restored =
                    (await store.FindByCorrelationIdAsync(correlationId))
                    .Single();

                restored.MarkProcessing(createdAt.AddSeconds(5));
                await store.SaveAsync(restored);
            }

            await using (var secondRestart = CreateContext(databasePath))
            {
                var store =
                    new EfMobileMoneyPayoutFundingAttemptStore(secondRestart);
                var restored =
                    (await store.FindByCorrelationIdAsync(correlationId))
                    .Single();

                Assert(
                    restored.Status == FundingAttemptStatus.Processing,
                    "Processing state must survive restart.");
                Assert(
                    restored.UpdatedAtUtc == createdAt.AddSeconds(5),
                    "Processing timestamp must survive restart.");

                restored.MarkFailed(
                    createdAt.AddSeconds(10),
                    "  provider-declined  ");
                await store.SaveAsync(restored);
            }

            await using (var thirdRestart = CreateContext(databasePath))
            {
                var restored =
                    (await new EfMobileMoneyPayoutFundingAttemptStore(thirdRestart)
                        .FindByCorrelationIdAsync(correlationId))
                    .Single();

                Assert(
                    restored.Status == FundingAttemptStatus.Failed,
                    "Failed state must survive repeated restart.");
                Assert(
                    restored.StatusReason == "provider-declined",
                    "Normalized failure reason must survive restart.");
                Assert(
                    restored.CreatedAtUtc == createdAt &&
                    restored.UpdatedAtUtc == createdAt.AddSeconds(10),
                    "Lifecycle timestamps must round-trip exactly.");
            }
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static async Task CreateDatabaseAsync(string databasePath)
    {
        await using var db = CreateContext(databasePath);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private static FundingAttemptDbContext CreateContext(
        string databasePath)
    {
        var options =
            new DbContextOptionsBuilder<FundingAttemptDbContext>()
                .UseSqlite("Data Source=" + databasePath)
                .Options;

        return new FundingAttemptDbContext(options);
    }

    private static string CreateDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(),
            string.Concat(
                "afw-momo-funding-",
                Guid.NewGuid().ToString("N"),
                ".db"));

    private static void DeleteDatabase(string databasePath)
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        var walPath = databasePath + "-wal";
        if (File.Exists(walPath))
        {
            File.Delete(walPath);
        }

        var shmPath = databasePath + "-shm";
        if (File.Exists(shmPath))
        {
            File.Delete(shmPath);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
