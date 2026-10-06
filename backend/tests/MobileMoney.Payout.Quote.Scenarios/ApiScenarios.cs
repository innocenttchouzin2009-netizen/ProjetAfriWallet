using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Application;
using MobileMoney.Production.Payout.Quote.Contracts;
using MobileMoney.Production.Payout.Quote.Domain;
using MobileMoney.Production.Payout.Quote.Extensions;

internal static class ApiScenarios
{
    public static Task RouteIsStable()
    {
        AssertEqual(
            "/api/v1/mobile-money/payouts/quote",
            MobileMoneyPayoutQuoteEndpointExtensions.Route);

        return Task.CompletedTask;
    }

    public static async Task ReturnsQuote()
    {
        var result = await MobileMoneyPayoutQuoteEndpointExtensions.HandleAsync(
            ValidRequest(),
            CreateOrchestrator(),
            new DefaultHttpContext(),
            CancellationToken.None);

        var ok = result as Ok<MobileMoneyPayoutQuoteResponse>
            ?? throw new InvalidOperationException("Expected HTTP 200 quote result.");

        Assert(ok.Value is not null, "Quote response is required.");
        AssertEqual("EUR", ok.Value!.SourceCurrency);
        AssertEqual("XAF", ok.Value.DestinationCurrency);
        AssertEqual(10_000L, ok.Value.SourceAmountMinor);
        AssertEqual(6_559_570L, ok.Value.DestinationAmountMinor);
    }

    public static async Task MapsInvalidRequest()
    {
        var invalid = ValidRequest() with { SourceAmountMinor = 0 };

        var result = await MobileMoneyPayoutQuoteEndpointExtensions.HandleAsync(
            invalid,
            CreateOrchestrator(),
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertError(
            result,
            StatusCodes.Status400BadRequest,
            "PAYOUT_QUOTE_INVALID_REQUEST");
    }

    public static async Task MapsIneligibleCorridor()
    {
        var orchestrator = CreateOrchestrator(
            eligibility: MobileMoneyPayoutEligibilityResult.Ineligible(
                MobileMoneyPayoutEligibilityCodes.CorridorDisabled));

        var result = await MobileMoneyPayoutQuoteEndpointExtensions.HandleAsync(
            ValidRequest(),
            orchestrator,
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertError(
            result,
            StatusCodes.Status422UnprocessableEntity,
            MobileMoneyPayoutEligibilityCodes.CorridorDisabled);
    }

    public static async Task MapsUnavailablePricing()
    {
        var unavailableFx = CreateOrchestrator(fxUnavailable: true);

        var fxResult = await MobileMoneyPayoutQuoteEndpointExtensions.HandleAsync(
            ValidRequest(),
            unavailableFx,
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertError(
            fxResult,
            StatusCodes.Status503ServiceUnavailable,
            MobileMoneyPayoutQuoteUnavailableException.ErrorCode);

        var missingFeePolicy = CreateOrchestrator(
            feePolicy: new MissingFeePolicy());

        var feeResult = await MobileMoneyPayoutQuoteEndpointExtensions.HandleAsync(
            ValidRequest(),
            missingFeePolicy,
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertError(
            feeResult,
            StatusCodes.Status503ServiceUnavailable,
            "PAYOUT_QUOTE_PRICING_UNAVAILABLE");
    }

    private static MobileMoneyPayoutQuoteOrchestrator CreateOrchestrator(
        MobileMoneyPayoutEligibilityResult? eligibility = null,
        MobileMoneyPayoutFxQuote? fxQuote = null,
        IMobileMoneyPayoutFeePolicy? feePolicy = null,
        bool fxUnavailable = false)
    {
        var resolvedFxQuote = fxUnavailable
            ? null
            : fxQuote ?? new MobileMoneyPayoutFxQuote(655.957m, 6_559_570);

        return new MobileMoneyPayoutQuoteOrchestrator(
            new StubEligibilityPolicy(
                eligibility ?? MobileMoneyPayoutEligibilityResult.Eligible()),
            new StubFxQuoteProvider(resolvedFxQuote),
            feePolicy ?? new StubFeePolicy(),
            new FixedClock(
                new DateTimeOffset(
                    2026,
                    10,
                    7,
                    0,
                    0,
                    0,
                    TimeSpan.Zero)),
            TimeSpan.FromMinutes(10));
    }

    private static CreateMobileMoneyPayoutQuoteRequest ValidRequest() =>
        new(
            "DE",
            "EUR",
            "CM",
            "XAF",
            "MTN-CM",
            10_000);

    private static void AssertError(
        IResult result,
        int expectedStatusCode,
        string expectedCode)
    {
        var error = result as JsonHttpResult<MobileMoneyPayoutErrorResponse>
            ?? throw new InvalidOperationException("Expected JSON error result.");

        AssertEqual(expectedStatusCode, error.StatusCode);
        Assert(error.Value is not null, "Error response is required.");
        AssertEqual(expectedCode, error.Value!.Code);
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

    private sealed class StubEligibilityPolicy(
        MobileMoneyPayoutEligibilityResult result)
        : IMobileMoneyPayoutEligibilityPolicy
    {
        public Task<MobileMoneyPayoutEligibilityResult> EvaluateAsync(
            MobileMoneyPayoutCorridor corridor,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class StubFxQuoteProvider(
        MobileMoneyPayoutFxQuote? quote)
        : IMobileMoneyPayoutFxQuoteProvider
    {
        public ValueTask<MobileMoneyPayoutFxQuote?> GetQuoteAsync(
            MobileMoneyPayoutFxQuoteRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(quote);
        }
    }

    private sealed class StubFeePolicy : IMobileMoneyPayoutFeePolicy
    {
        public ValueTask<IReadOnlyList<MobileMoneyPayoutFee>> CalculateAsync(
            MobileMoneyPayoutFeeContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<MobileMoneyPayoutFee> fees =
            [
                new MobileMoneyPayoutFee(
                    "SERVICE_FEE",
                    250,
                    "EUR")
            ];

            return ValueTask.FromResult(fees);
        }
    }

    private sealed class MissingFeePolicy : IMobileMoneyPayoutFeePolicy
    {
        public ValueTask<IReadOnlyList<MobileMoneyPayoutFee>> CalculateAsync(
            MobileMoneyPayoutFeeContext context,
            CancellationToken cancellationToken = default) =>
            throw new KeyNotFoundException("Pricing is not configured.");
    }

    private sealed class FixedClock(DateTimeOffset utcNow)
        : IMobileMoneyPayoutClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
