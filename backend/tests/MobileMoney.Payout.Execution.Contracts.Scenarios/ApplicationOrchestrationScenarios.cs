using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Contracts;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Funding.Domain;

internal static class ApplicationOrchestrationScenarios
{
    public static async Task RunAsync()
    {
        await PrepareCreatesBindingWithoutProviderSubmissionAsync();
        await ReplayReturnsSameBindingAsync();
        await ReusedKeyWithDifferentIntentIsRejectedAsync();
        await MissingQuoteIsRejectedAsync();
        await MissingFundingIsRejectedAsync();
        await InvalidBindingIsRejectedAsync();
    }

    private static async Task PrepareCreatesBindingWithoutProviderSubmissionAsync()
    {
        var now = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var quote = CreateQuote(now.AddMinutes(5));
        var funding = CreateFunding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
        var store = new RecordingBindingStore();
        var orchestrator = CreateOrchestrator(now, quote, funding, store);

        var response = await orchestrator.PrepareAsync(
            new PrepareMobileMoneyPayoutExecutionRequest(
                quote.QuoteId,
                funding.CorrelationId,
                "exec-api-1"));

        Assert(response.BindingId != Guid.Empty, "Execution binding id must be assigned.");
        Assert(response.QuoteId == quote.QuoteId, "Execution quote id mismatch.");
        Assert(
            response.FundingCorrelationId == funding.CorrelationId,
            "Execution funding correlation mismatch.");
        Assert(response.IdempotencyKey == "exec-api-1", "Idempotency key mismatch.");
        Assert(response.Status == "Prepared", "Execution must remain prepared only.");
        Assert(!response.IsReplay, "First preparation cannot be marked as replay.");
        Assert(response.PreparedAtUtc == now, "Prepared timestamp mismatch.");
        Assert(store.SaveCount == 1, "Prepared binding must be stored exactly once.");
    }

    private static async Task ReplayReturnsSameBindingAsync()
    {
        var now = new DateTimeOffset(2026, 10, 11, 19, 5, 0, TimeSpan.Zero);
        var quote = CreateQuote(now.AddMinutes(5));
        var funding = CreateFunding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
        var store = new RecordingBindingStore();
        var orchestrator = CreateOrchestrator(now, quote, funding, store);
        var request = new PrepareMobileMoneyPayoutExecutionRequest(
            quote.QuoteId,
            funding.CorrelationId,
            "exec-api-replay");

        var first = await orchestrator.PrepareAsync(request);
        var replay = await orchestrator.PrepareAsync(request);

        Assert(replay.BindingId == first.BindingId, "Replay must return the original binding.");
        Assert(replay.IsReplay, "Replay must be explicitly identified.");
        Assert(store.SaveCount == 1, "Replay must not save a second binding.");
    }

    private static async Task ReusedKeyWithDifferentIntentIsRejectedAsync()
    {
        var now = new DateTimeOffset(2026, 10, 11, 19, 10, 0, TimeSpan.Zero);
        var quote = CreateQuote(now.AddMinutes(5));
        var funding = CreateFunding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
        var store = new RecordingBindingStore();
        var orchestrator = CreateOrchestrator(now, quote, funding, store);
        var key = "exec-api-conflict";

        await orchestrator.PrepareAsync(
            new PrepareMobileMoneyPayoutExecutionRequest(
                quote.QuoteId,
                funding.CorrelationId,
                key));

        await ExpectCodeAsync(
            MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
            () => orchestrator.PrepareAsync(
                new PrepareMobileMoneyPayoutExecutionRequest(
                    Guid.NewGuid(),
                    funding.CorrelationId,
                    key)));
    }

    private static async Task MissingQuoteIsRejectedAsync()
    {
        var now = new DateTimeOffset(2026, 10, 11, 19, 15, 0, TimeSpan.Zero);
        var funding = CreateFunding(10_000, "EUR");
        var orchestrator = CreateOrchestrator(
            now,
            quote: null,
            funding,
            new RecordingBindingStore());

        await ExpectCodeAsync(
            MobileMoneyPayoutExecutionErrorCodes.QuoteNotFound,
            () => orchestrator.PrepareAsync(
                new PrepareMobileMoneyPayoutExecutionRequest(
                    Guid.NewGuid(),
                    funding.CorrelationId,
                    "exec-api-missing-quote")));
    }

    private static async Task MissingFundingIsRejectedAsync()
    {
        var now = new DateTimeOffset(2026, 10, 11, 19, 20, 0, TimeSpan.Zero);
        var quote = CreateQuote(now.AddMinutes(5));
        var orchestrator = CreateOrchestrator(
            now,
            quote,
            funding: null,
            new RecordingBindingStore());

        await ExpectCodeAsync(
            MobileMoneyPayoutExecutionErrorCodes.FundingNotFound,
            () => orchestrator.PrepareAsync(
                new PrepareMobileMoneyPayoutExecutionRequest(
                    quote.QuoteId,
                    Guid.NewGuid(),
                    "exec-api-missing-funding")));
    }

    private static async Task InvalidBindingIsRejectedAsync()
    {
        var now = new DateTimeOffset(2026, 10, 11, 19, 25, 0, TimeSpan.Zero);
        var quote = CreateQuote(now);
        var funding = CreateFunding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
        var orchestrator = CreateOrchestrator(
            now,
            quote,
            funding,
            new RecordingBindingStore());

        await ExpectCodeAsync(
            MobileMoneyPayoutExecutionErrorCodes.BindingInvalid,
            () => orchestrator.PrepareAsync(
                new PrepareMobileMoneyPayoutExecutionRequest(
                    quote.QuoteId,
                    funding.CorrelationId,
                    "exec-api-expired")));
    }

    private static MobileMoneyPayoutExecutionOrchestrator CreateOrchestrator(
        DateTimeOffset now,
        MobileMoneyPayoutExecutionQuoteBinding? quote,
        MobileMoneyPayoutExecutionFundingBinding? funding,
        RecordingBindingStore store) =>
        new(
            new StubQuoteReader(quote),
            new StubFundingReader(funding),
            store,
            new FixedClock(now));

    private static MobileMoneyPayoutExecutionQuoteBinding CreateQuote(
        DateTimeOffset expiresAtUtc) =>
        MobileMoneyPayoutExecutionQuoteBinding.Create(
            Guid.NewGuid(),
            "DE",
            "EUR",
            9_500,
            500,
            10_000,
            "CM",
            "XAF",
            6_200_000,
            "MTN_CM",
            expiresAtUtc);

    private static MobileMoneyPayoutExecutionFundingBinding CreateFunding(
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
                    currencyCode),
            });

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
            Assert(
                exception.Code == expectedCode,
                $"Expected error code {expectedCode}, got {exception.Code}.");
            return;
        }

        throw new InvalidOperationException(
            $"Expected {nameof(MobileMoneyPayoutExecutionPreparationException)}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class StubQuoteReader(MobileMoneyPayoutExecutionQuoteBinding? quote)
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

    private sealed class StubFundingReader(MobileMoneyPayoutExecutionFundingBinding? funding)
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

    private sealed class RecordingBindingStore : IMobileMoneyPayoutExecutionBindingStore
    {
        private readonly Dictionary<string, MobileMoneyPayoutExecutionBinding> _bindings =
            new(StringComparer.Ordinal);

        public int SaveCount { get; private set; }

        public Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
            MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _bindings.TryGetValue(idempotencyKey.Value, out var binding);
            return Task.FromResult(binding);
        }

        public Task SaveAsync(
            MobileMoneyPayoutExecutionBinding binding,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _bindings[binding.IdempotencyKey.Value] = binding;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IMobileMoneyPayoutClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
