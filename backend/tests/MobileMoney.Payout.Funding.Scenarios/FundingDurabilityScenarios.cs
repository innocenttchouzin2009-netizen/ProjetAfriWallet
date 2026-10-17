using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;
using MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

internal static class FundingDurabilityScenarios
{
    public static async Task RunAsync()
    {
        await RestartReplayReturnsSamePlanAsync();
        await ConflictingReplayAfterRestartFailsClosedAsync();
        await ConcurrentIdenticalPlanningConvergesAsync();
        await PartialPersistedPlanFailsClosedAsync();
    }

    private static async Task RestartReplayReturnsSamePlanAsync()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var correlationId = Guid.NewGuid();
            var originalRequestedAt =
                new DateTimeOffset(2026, 10, 17, 6, 0, 0, TimeSpan.Zero);
            var command = Command(correlationId, originalRequestedAt);

            MobileMoneyPayoutFundingPlan first;
            Guid[] firstAttemptIds;

            await using (var db = CreateDbContext(path))
            {
                var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
                var service = CreateService(store, available: true);
                first = await service.PlanAsync(command);
                firstAttemptIds = (await store.FindByCorrelationIdAsync(correlationId))
                    .Select(x => x.Id)
                    .ToArray();
            }

            SqliteConnection.ClearAllPools();

            await using (var db = CreateDbContext(path))
            {
                var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
                var service = CreateService(store, available: false);
                var replay = await service.PlanAsync(
                    Command(
                        correlationId,
                        originalRequestedAt.AddHours(1)));

                var replayAttemptIds =
                    (await store.FindByCorrelationIdAsync(correlationId))
                    .Select(x => x.Id)
                    .ToArray();

                Assert(first.PlannedAtUtc == replay.PlannedAtUtc,
                    "Funding replay must preserve original PlannedAtUtc.");
                Assert(first.Allocations.SequenceEqual(replay.Allocations),
                    "Funding replay must preserve authoritative allocations.");
                Assert(firstAttemptIds.SequenceEqual(replayAttemptIds),
                    "Funding replay must preserve original attempt ids.");
                Assert(replayAttemptIds.Length == 2,
                    "Funding replay must not duplicate attempts.");
            }
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static async Task ConflictingReplayAfterRestartFailsClosedAsync()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var correlationId = Guid.NewGuid();
            var requestedAt =
                new DateTimeOffset(2026, 10, 17, 6, 10, 0, TimeSpan.Zero);

            await using (var db = CreateDbContext(path))
            {
                var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
                await CreateService(store, available: true)
                    .PlanAsync(Command(correlationId, requestedAt));
            }

            SqliteConnection.ClearAllPools();

            await using (var db = CreateDbContext(path))
            {
                var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
                var service = CreateService(store, available: false);
                var conflicting = new PlanMobileMoneyPayoutFundingCommand(
                    correlationId,
                    10_001,
                    "EUR",
                    new[]
                    {
                        FundingAllocation.Create(
                            "wallet-main",
                            FundingSourceType.Wallet,
                            4_001,
                            "EUR"),
                        FundingAllocation.Create(
                            "google-pay",
                            FundingSourceType.GooglePay,
                            6_000,
                            "EUR")
                    },
                    requestedAt.AddMinutes(1));

                await ExpectAsync<InvalidOperationException>(
                    () => service.PlanAsync(conflicting));
            }
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static async Task ConcurrentIdenticalPlanningConvergesAsync()
    {
        var path = TemporaryDatabasePath();
        try
        {
            await using (var init = CreateDbContext(path))
                await init.Database.EnsureCreatedAsync();

            var correlationId = Guid.NewGuid();
            var requestedAt =
                new DateTimeOffset(2026, 10, 17, 6, 20, 0, TimeSpan.Zero);
            var command = Command(correlationId, requestedAt);

            await using var db1 = CreateDbContext(path);
            await using var db2 = CreateDbContext(path);
            var store1 = new EfMobileMoneyPayoutFundingAttemptStore(db1);
            var store2 = new EfMobileMoneyPayoutFundingAttemptStore(db2);

            var plans = await Task.WhenAll(
                CreateService(store1, available: true).PlanAsync(command),
                CreateService(store2, available: true).PlanAsync(command));

            Assert(plans[0].PlannedAtUtc == plans[1].PlannedAtUtc,
                "Concurrent funding plans must converge on one timestamp.");
            Assert(plans[0].Allocations.SequenceEqual(plans[1].Allocations),
                "Concurrent funding plans must converge on one allocation set.");

            await using var verifyDb = CreateDbContext(path);
            var verifyStore = new EfMobileMoneyPayoutFundingAttemptStore(verifyDb);
            var attempts =
                await verifyStore.FindByCorrelationIdAsync(correlationId);

            Assert(attempts.Count == 2,
                "Concurrent funding requests must not duplicate attempts.");
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static async Task PartialPersistedPlanFailsClosedAsync()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var correlationId = Guid.NewGuid();
            var requestedAt =
                new DateTimeOffset(2026, 10, 17, 6, 30, 0, TimeSpan.Zero);

            await using (var db = CreateDbContext(path))
            {
                var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
                await CreateService(store, available: true)
                    .PlanAsync(Command(correlationId, requestedAt));
            }

            await using (var corruptDb = CreateDbContext(path))
            {
                var row = await corruptDb.Attempts
                    .Where(x => x.CorrelationId == correlationId)
                    .OrderByDescending(x => x.Sequence)
                    .FirstAsync();
                corruptDb.Attempts.Remove(row);
                await corruptDb.SaveChangesAsync();
            }

            await using (var db = CreateDbContext(path))
            {
                var store = new EfMobileMoneyPayoutFundingAttemptStore(db);
                await ExpectAsync<InvalidOperationException>(
                    () => store.FindByCorrelationIdAsync(correlationId));
            }
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static MobileMoneyPayoutFundingPlanningService CreateService(
        IMobileMoneyPayoutFundingAttemptStore store,
        bool available) =>
        new(
            new DurableSourceReader(available),
            store,
            new MobileMoneyPayoutSplitFundingValidator());

    private static PlanMobileMoneyPayoutFundingCommand Command(
        Guid correlationId,
        DateTimeOffset requestedAtUtc) =>
        new(
            correlationId,
            10_000,
            "EUR",
            new[]
            {
                FundingAllocation.Create(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    4_000,
                    "EUR"),
                FundingAllocation.Create(
                    "google-pay",
                    FundingSourceType.GooglePay,
                    6_000,
                    "EUR")
            },
            requestedAtUtc);

    private static MobileMoneyPayoutFundingDbContext CreateDbContext(
        string path)
    {
        var options =
            new DbContextOptionsBuilder<MobileMoneyPayoutFundingDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=5")
                .Options;
        return new MobileMoneyPayoutFundingDbContext(options);
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(),
            $"afw-momo-funding-{Guid.NewGuid():N}.db");

    private static void Cleanup(string path)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path))
            File.Delete(path);
    }

    private static async Task ExpectAsync<T>(Func<Task> action)
        where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected {typeof(T).Name}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class DurableSourceReader(bool available)
        : IMobileMoneyPayoutFundingSourceReader
    {
        public Task<FundingSourceSnapshot?> GetAsync(
            string sourceId,
            FundingSourceType sourceType,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!available)
                return Task.FromResult<FundingSourceSnapshot?>(null);

            FundingSourceSnapshot? snapshot = sourceId switch
            {
                "wallet-main" => new(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    "EUR",
                    true,
                    10_000),
                "google-pay" => new(
                    "google-pay",
                    FundingSourceType.GooglePay,
                    "EUR",
                    true,
                    null),
                _ => null
            };

            return Task.FromResult(snapshot);
        }
    }
}
