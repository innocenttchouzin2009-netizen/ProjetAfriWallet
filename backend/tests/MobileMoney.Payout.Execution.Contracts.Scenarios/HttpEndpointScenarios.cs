using Microsoft.AspNetCore.Http;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Contracts;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Execution.Extensions;
using MobileMoney.Production.Payout.Funding.Domain;

internal static class HttpEndpointScenarios
{
    public static async Task RunAsync()
    {
        RouteIsVersioned();
        await MissingIdempotencyHeaderIsRejectedAsync();
        await RequestAndReplayAreIdempotentAsync();
        await ConflictingReplayIsRejectedAsync();
    }

    private static void RouteIsVersioned()
    {
        Assert(
            MobileMoneyPayoutExecutionEndpointExtensions.Route
                == "/api/v1/mobile-money/payouts/execution/prepare",
            "Execution API route must remain versioned under /api/v1.");
    }

    private static async Task MissingIdempotencyHeaderIsRejectedAsync()
    {
        var fixture = CreateFixture();

        var result = await MobileMoneyPayoutExecutionEndpointExtensions.HandleAsync(
            new PrepareMobileMoneyPayoutExecutionHttpRequest(
                fixture.Quote.QuoteId,
                fixture.Funding.CorrelationId),
            fixture.Orchestrator,
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertStatus(result, StatusCodes.Status400BadRequest);
        AssertErrorCode(
            result,
            MobileMoneyPayoutExecutionErrorCodes.IdempotencyRequired);
    }

    private static async Task RequestAndReplayAreIdempotentAsync()
    {
        var fixture = CreateFixture();
        var request = new PrepareMobileMoneyPayoutExecutionHttpRequest(
            fixture.Quote.QuoteId,
            fixture.Funding.CorrelationId);

        var firstContext = ContextWithIdempotencyKey("http-exec-replay");
        var first = await MobileMoneyPayoutExecutionEndpointExtensions.HandleAsync(
            request,
            fixture.Orchestrator,
            firstContext,
            CancellationToken.None);

        var replayContext = ContextWithIdempotencyKey("http-exec-replay");
        var replay = await MobileMoneyPayoutExecutionEndpointExtensions.HandleAsync(
            request,
            fixture.Orchestrator,
            replayContext,
            CancellationToken.None);

        AssertStatus(first, StatusCodes.Status200OK);
        AssertStatus(replay, StatusCodes.Status200OK);

        var firstValue = Value<PrepareMobileMoneyPayoutExecutionResponse>(first);
        var replayValue = Value<PrepareMobileMoneyPayoutExecutionResponse>(replay);

        Assert(!firstValue.IsReplay, "First execution preparation cannot be a replay.");
        Assert(replayValue.IsReplay, "Repeated idempotency key must be marked as replay.");
        Assert(
            replayValue.BindingId == firstValue.BindingId,
            "Idempotent replay must return the original binding id.");
    }

    private static async Task ConflictingReplayIsRejectedAsync()
    {
        var fixture = CreateFixture();
        var key = "http-exec-conflict";

        await MobileMoneyPayoutExecutionEndpointExtensions.HandleAsync(
            new PrepareMobileMoneyPayoutExecutionHttpRequest(
                fixture.Quote.QuoteId,
                fixture.Funding.CorrelationId),
            fixture.Orchestrator,
            ContextWithIdempotencyKey(key),
            CancellationToken.None);

        var result = await MobileMoneyPayoutExecutionEndpointExtensions.HandleAsync(
            new PrepareMobileMoneyPayoutExecutionHttpRequest(
                Guid.NewGuid(),
                fixture.Funding.CorrelationId),
            fixture.Orchestrator,
            ContextWithIdempotencyKey(key),
            CancellationToken.None);

        AssertStatus(result, StatusCodes.Status409Conflict);
        AssertErrorCode(
            result,
            MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict);
    }

    private static Fixture CreateFixture()
    {
        var now = new DateTimeOffset(2026, 10, 12, 6, 0, 0, TimeSpan.Zero);
        var quote = MobileMoneyPayoutExecutionQuoteBinding.Create(
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
            now.AddMinutes(5));

        var funding = MobileMoneyPayoutExecutionFundingBinding.Create(
            Guid.NewGuid(),
            quote.TotalSourceDebitMinor,
            quote.SourceCurrency,
            new[]
            {
                MobileMoneyPayoutExecutionFundingAllocation.Create(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    quote.TotalSourceDebitMinor,
                    quote.SourceCurrency),
            });

        var orchestrator = new MobileMoneyPayoutExecutionOrchestrator(
            new StubQuoteReader(quote),
            new StubFundingReader(funding),
            new RecordingBindingStore(),
            new FixedClock(now));

        return new Fixture(quote, funding, orchestrator);
    }

    private static DefaultHttpContext ContextWithIdempotencyKey(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[
            MobileMoneyPayoutExecutionEndpointExtensions.IdempotencyHeaderName] = value;
        return context;
    }

    private static void AssertStatus(IResult result, int expected)
    {
        if (result is not IStatusCodeHttpResult statusResult
            || statusResult.StatusCode != expected)
        {
            throw new InvalidOperationException(
                $"Expected HTTP {expected}.");
        }
    }

    private static void AssertErrorCode(IResult result, string expected)
    {
        var error = Value<MobileMoneyPayoutErrorResponse>(result);
        Assert(error.Code == expected, $"Expected error code {expected}, got {error.Code}.");
    }

    private static T Value<T>(IResult result)
    {
        if (result is not IValueHttpResult valueResult
            || valueResult.Value is not T value)
        {
            throw new InvalidOperationException(
                $"Expected result value of type {typeof(T).Name}.");
        }

        return value;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record Fixture(
        MobileMoneyPayoutExecutionQuoteBinding Quote,
        MobileMoneyPayoutExecutionFundingBinding Funding,
        MobileMoneyPayoutExecutionOrchestrator Orchestrator);

    private sealed class StubQuoteReader(MobileMoneyPayoutExecutionQuoteBinding quote)
        : IMobileMoneyPayoutExecutionQuoteBindingReader
    {
        public Task<MobileMoneyPayoutExecutionQuoteBinding?> FindByIdAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<MobileMoneyPayoutExecutionQuoteBinding?>(
                quote.QuoteId == quoteId ? quote : null);
        }
    }

    private sealed class StubFundingReader(MobileMoneyPayoutExecutionFundingBinding funding)
        : IMobileMoneyPayoutExecutionFundingBindingReader
    {
        public Task<MobileMoneyPayoutExecutionFundingBinding?> FindByCorrelationIdAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<MobileMoneyPayoutExecutionFundingBinding?>(
                funding.CorrelationId == correlationId ? funding : null);
        }
    }

    private sealed class RecordingBindingStore : IMobileMoneyPayoutExecutionBindingStore
    {
        private readonly Dictionary<string, MobileMoneyPayoutExecutionBinding> _items =
            new(StringComparer.Ordinal);

        public Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
            MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue(idempotencyKey.Value, out var binding);
            return Task.FromResult(binding);
        }

        public Task SaveAsync(
            MobileMoneyPayoutExecutionBinding binding,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items[binding.IdempotencyKey.Value] = binding;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IMobileMoneyPayoutClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
