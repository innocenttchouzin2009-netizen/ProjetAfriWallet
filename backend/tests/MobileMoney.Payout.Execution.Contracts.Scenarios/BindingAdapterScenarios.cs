using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Execution.Infrastructure;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Domain;

internal static class BindingAdapterScenarios
{
    public static async Task RunAsync()
    {
        await QuoteAdapterMapsStoredQuoteAsync();
        await QuoteAdapterReturnsNullWhenMissingAsync();
        await FundingAdapterMapsPlannedAttemptsAsync();
        await FundingAdapterReturnsNullWhenMissingAsync();
        await FundingAdapterRejectsProgressedAttemptsAsync();
    }

    private static async Task QuoteAdapterMapsStoredQuoteAsync()
    {
        var createdAtUtc =
            new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
        var quote = MobileMoneyPayoutQuote.Create(
            new MobileMoneyPayoutCorridor(
                "DE",
                "EUR",
                "CM",
                "XAF",
                "MTN_CM"),
            9_500,
            6_200_000,
            652.631578m,
            new[]
            {
                new MobileMoneyPayoutFee("SERVICE_FEE", 500, "EUR"),
            },
            createdAtUtc,
            createdAtUtc.AddMinutes(5));

        var store = new RecordingQuoteStore();
        await store.SaveAsync(quote);

        var reader =
            new StoreBackedMobileMoneyPayoutExecutionQuoteBindingReader(store);
        var binding = await reader.FindByIdAsync(quote.QuoteId);

        Assert(binding is not null, "Stored quote must bind for execution.");
        Assert(binding!.QuoteId == quote.QuoteId, "Quote id mapping mismatch.");
        Assert(binding.SourceCurrency == "EUR", "Source currency mapping mismatch.");
        Assert(binding.SourceAmountMinor == 9_500, "Source amount mapping mismatch.");
        Assert(binding.TotalFeeMinor == 500, "Fee mapping mismatch.");
        Assert(binding.TotalSourceDebitMinor == 10_000, "Debit mapping mismatch.");
        Assert(binding.DestinationCurrency == "XAF", "Destination currency mapping mismatch.");
        Assert(binding.DestinationAmountMinor == 6_200_000, "Destination amount mapping mismatch.");
        Assert(binding.ExpiresAtUtc == quote.ExpiresAtUtc, "Expiry mapping mismatch.");
    }

    private static async Task QuoteAdapterReturnsNullWhenMissingAsync()
    {
        var reader =
            new StoreBackedMobileMoneyPayoutExecutionQuoteBindingReader(
                new RecordingQuoteStore());

        var binding = await reader.FindByIdAsync(Guid.NewGuid());

        Assert(binding is null, "Missing quote must not produce an execution binding.");
    }

    private static async Task FundingAdapterMapsPlannedAttemptsAsync()
    {
        var correlationId = Guid.NewGuid();
        var plannedAtUtc =
            new DateTimeOffset(2026, 10, 11, 16, 5, 0, TimeSpan.Zero);
        var store = new RecordingFundingAttemptStore();

        await store.SaveAsync(FundingAttempt.Create(
            Guid.NewGuid(),
            correlationId,
            FundingAllocation.Create(
                "wallet-main",
                FundingSourceType.Wallet,
                4_000,
                "EUR"),
            plannedAtUtc));

        await store.SaveAsync(FundingAttempt.Create(
            Guid.NewGuid(),
            correlationId,
            FundingAllocation.Create(
                "apple-pay",
                FundingSourceType.ApplePay,
                6_000,
                "EUR"),
            plannedAtUtc));

        var reader =
            new StoreBackedMobileMoneyPayoutExecutionFundingBindingReader(store);
        var binding = await reader.FindByCorrelationIdAsync(correlationId);

        Assert(binding is not null, "Planned funding attempts must bind for execution.");
        Assert(binding!.CorrelationId == correlationId, "Funding correlation mapping mismatch.");
        Assert(binding.RequiredAmountMinor == 10_000, "Funding amount mapping mismatch.");
        Assert(binding.CurrencyCode == "EUR", "Funding currency mapping mismatch.");
        Assert(binding.Allocations.Count == 2, "Funding allocation count mismatch.");
        Assert(
            binding.Allocations.Sum(allocation => allocation.AmountMinor) == 10_000,
            "Funding allocations must preserve exact coverage.");
    }

    private static async Task FundingAdapterReturnsNullWhenMissingAsync()
    {
        var reader =
            new StoreBackedMobileMoneyPayoutExecutionFundingBindingReader(
                new RecordingFundingAttemptStore());

        var binding = await reader.FindByCorrelationIdAsync(Guid.NewGuid());

        Assert(binding is null, "Missing funding plan must not produce an execution binding.");
    }

    private static async Task FundingAdapterRejectsProgressedAttemptsAsync()
    {
        var correlationId = Guid.NewGuid();
        var plannedAtUtc =
            new DateTimeOffset(2026, 10, 11, 16, 10, 0, TimeSpan.Zero);
        var attempt = FundingAttempt.Create(
            Guid.NewGuid(),
            correlationId,
            FundingAllocation.Create(
                "wallet-main",
                FundingSourceType.Wallet,
                10_000,
                "EUR"),
            plannedAtUtc);
        attempt.MarkProcessing(plannedAtUtc.AddSeconds(1));

        var store = new RecordingFundingAttemptStore();
        await store.SaveAsync(attempt);

        var reader =
            new StoreBackedMobileMoneyPayoutExecutionFundingBindingReader(store);

        await ExpectAsync<InvalidOperationException>(
            () => reader.FindByCorrelationIdAsync(correlationId));
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

        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class RecordingQuoteStore : IMobileMoneyPayoutQuoteStore
    {
        private readonly Dictionary<Guid, MobileMoneyPayoutQuote> _quotes = new();
        private readonly Dictionary<Guid, Guid> _fundingLinks = new();

        public Task SaveAsync(
            MobileMoneyPayoutQuote quote,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _quotes[quote.QuoteId] = quote;
            return Task.CompletedTask;
        }

        public Task<MobileMoneyPayoutQuote?> FindByIdAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _quotes.TryGetValue(quoteId, out var quote);
            return Task.FromResult(quote);
        }

        public Task LinkFundingCorrelationAsync(
            Guid quoteId,
            Guid fundingCorrelationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _fundingLinks[quoteId] = fundingCorrelationId;
            return Task.CompletedTask;
        }

        public Task<Guid?> FindFundingCorrelationIdAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                _fundingLinks.TryGetValue(quoteId, out var correlationId)
                    ? (Guid?)correlationId
                    : null);
        }

        public Task<Guid?> FindQuoteIdByFundingCorrelationIdAsync(
            Guid fundingCorrelationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quoteId = _fundingLinks
                .Where(entry => entry.Value == fundingCorrelationId)
                .Select(entry => (Guid?)entry.Key)
                .SingleOrDefault();
            return Task.FromResult(quoteId);
        }
    }

    private sealed class RecordingFundingAttemptStore
        : IMobileMoneyPayoutFundingAttemptStore
    {
        private readonly List<FundingAttempt> _attempts = [];

        public Task SaveAsync(
            FundingAttempt attempt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _attempts.Add(attempt);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<FundingAttempt> result = _attempts
                .Where(attempt => attempt.CorrelationId == correlationId)
                .ToArray();

            return Task.FromResult(result);
        }
    }
}
