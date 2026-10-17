using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Contracts;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;
using MobileMoney.Production.Payout.Funding.Domain;

internal static class ExecutionDurabilityScenarios
{
    public static async Task RunAsync()
    {
        await RestartReplayReturnsSameBindingAsync();
        await ConflictingReplayAfterRestartFailsClosedAsync();
        await ConcurrentIdenticalPreparationConvergesAsync();
    }

    private static async Task RestartReplayReturnsSameBindingAsync()
    {
        var path = TemporaryDatabasePath();
        try
        {
            var now =
                new DateTimeOffset(2026, 10, 17, 6, 40, 0, TimeSpan.Zero);
            var quote = Quote(now.AddMinutes(10));
            var funding = Funding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
            const string key = "beta2.60-restart-replay";

            PrepareMobileMoneyPayoutExecutionResponse first;

            await using (var db = CreateDbContext(path))
            {
                var orchestrator = CreateOrchestrator(
                    quote,
                    funding,
                    new EfMobileMoneyPayoutExecutionBindingStore(db),
                    now);

                first = await orchestrator.PrepareAsync(
                    new PrepareMobileMoneyPayoutExecutionRequest(
                        quote.QuoteId,
                        funding.CorrelationId,
                        key));
            }

            SqliteConnection.ClearAllPools();

            await using (var db = CreateDbContext(path))
            {
                var orchestrator = CreateOrchestrator(
                    quote: null,
                    funding: null,
                    new EfMobileMoneyPayoutExecutionBindingStore(db),
                    now.AddHours(1));

                var replay = await orchestrator.PrepareAsync(
                    new PrepareMobileMoneyPayoutExecutionRequest(
                        quote.QuoteId,
                        funding.CorrelationId,
                        key));

                Assert(replay.IsReplay,
                    "Restart replay must be reported as replay.");
                Assert(replay.BindingId == first.BindingId,
                    "Restart replay must preserve binding id.");
                Assert(replay.PreparedAtUtc == first.PreparedAtUtc,
                    "Restart replay must preserve PreparedAtUtc.");
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
            var now =
                new DateTimeOffset(2026, 10, 17, 6, 50, 0, TimeSpan.Zero);
            var quote = Quote(now.AddMinutes(10));
            var funding = Funding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
            const string key = "beta2.60-conflict";

            await using (var db = CreateDbContext(path))
            {
                await CreateOrchestrator(
                    quote,
                    funding,
                    new EfMobileMoneyPayoutExecutionBindingStore(db),
                    now)
                    .PrepareAsync(
                        new PrepareMobileMoneyPayoutExecutionRequest(
                            quote.QuoteId,
                            funding.CorrelationId,
                            key));
            }

            SqliteConnection.ClearAllPools();

            await using (var db = CreateDbContext(path))
            {
                var orchestrator = CreateOrchestrator(
                    quote: null,
                    funding: null,
                    new EfMobileMoneyPayoutExecutionBindingStore(db),
                    now.AddHours(1));

                await ExpectCodeAsync(
                    MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
                    () => orchestrator.PrepareAsync(
                        new PrepareMobileMoneyPayoutExecutionRequest(
                            Guid.NewGuid(),
                            funding.CorrelationId,
                            key)));
            }
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static async Task ConcurrentIdenticalPreparationConvergesAsync()
    {
        var path = TemporaryDatabasePath();
        try
        {
            await using (var init = CreateDbContext(path))
                await init.Database.EnsureCreatedAsync();

            var now =
                new DateTimeOffset(2026, 10, 17, 7, 0, 0, TimeSpan.Zero);
            var quote = Quote(now.AddMinutes(10));
            var funding = Funding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
            const string key = "beta2.60-concurrent";

            await using var db1 = CreateDbContext(path);
            await using var db2 = CreateDbContext(path);

            var request = new PrepareMobileMoneyPayoutExecutionRequest(
                quote.QuoteId,
                funding.CorrelationId,
                key);

            var responses = await Task.WhenAll(
                CreateOrchestrator(
                    quote,
                    funding,
                    new EfMobileMoneyPayoutExecutionBindingStore(db1),
                    now)
                    .PrepareAsync(request),
                CreateOrchestrator(
                    quote,
                    funding,
                    new EfMobileMoneyPayoutExecutionBindingStore(db2),
                    now)
                    .PrepareAsync(request));

            Assert(responses[0].BindingId == responses[1].BindingId,
                "Concurrent preparation must converge on one binding id.");
            Assert(responses[0].PreparedAtUtc == responses[1].PreparedAtUtc,
                "Concurrent preparation must converge on one timestamp.");

            await using var verifyDb = CreateDbContext(path);
            Assert(await verifyDb.Bindings.CountAsync() == 1,
                "Concurrent preparation must persist exactly one binding.");
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static MobileMoneyPayoutExecutionOrchestrator CreateOrchestrator(
        MobileMoneyPayoutExecutionQuoteBinding? quote,
        MobileMoneyPayoutExecutionFundingBinding? funding,
        IMobileMoneyPayoutExecutionBindingStore store,
        DateTimeOffset now) =>
        new(
            new StubQuoteReader(quote),
            new StubFundingReader(funding),
            store,
            new FixedClock(now));

    private static MobileMoneyPayoutExecutionQuoteBinding Quote(
        DateTimeOffset expiresAtUtc) =>
        MobileMoneyPayoutExecutionQuoteBinding.Create(
            Guid.NewGuid(),
            "EUR",
            9_500,
            500,
            10_000,
            "XAF",
            6_200_000,
            expiresAtUtc);

    private static MobileMoneyPayoutExecutionFundingBinding Funding(
        long requiredAmountMinor,
        string currencyCode) =>
        MobileMoneyPayoutExecutionFundingBinding.Create(
            Guid.NewGuid(),
            requiredAmountMinor,
            currencyCode,
            new[]
            {
                MobileMoneyPayoutExecutionFundingAllocation.Create(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    requiredAmountMinor,
                    currencyCode)
            });

    private static MobileMoneyPayoutExecutionDbContext CreateDbContext(
        string path)
    {
        var options =
            new DbContextOptionsBuilder<MobileMoneyPayoutExecutionDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=5")
                .Options;
        return new MobileMoneyPayoutExecutionDbContext(options);
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(
            Path.GetTempPath(),
            $"afw-momo-execution-{Guid.NewGuid():N}.db");

    private static void Cleanup(string path)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path))
            File.Delete(path);
    }

    private static async Task ExpectCodeAsync(
        string expectedCode,
        Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (MobileMoneyPayoutExecutionPreparationException exception)
        {
            Assert(exception.Code == expectedCode,
                $"Expected {expectedCode}, got {exception.Code}.");
            return;
        }

        throw new InvalidOperationException(
            "Expected execution preparation exception.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class StubQuoteReader(
        MobileMoneyPayoutExecutionQuoteBinding? quote)
        : IMobileMoneyPayoutExecutionQuoteBindingReader
    {
        public Task<MobileMoneyPayoutExecutionQuoteBinding?> FindByIdAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                quote?.QuoteId == quoteId ? quote : null);
        }
    }

    private sealed class StubFundingReader(
        MobileMoneyPayoutExecutionFundingBinding? funding)
        : IMobileMoneyPayoutExecutionFundingBindingReader
    {
        public Task<MobileMoneyPayoutExecutionFundingBinding?> FindByCorrelationIdAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                funding?.CorrelationId == correlationId ? funding : null);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow)
        : IMobileMoneyPayoutClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
