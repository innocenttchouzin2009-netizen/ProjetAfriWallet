using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Application;
using MobileMoney.Production.Payout.Quote.Contracts;
using MobileMoney.Production.Payout.Quote.Domain;

internal static class OrchestrationScenarios
{
    public static async Task ComposesQuote()
    {
        var clock = new FixedClock(Utc(2026, 10, 6, 10, 0));
        var eligibility = new RecordingEligibilityPolicy(
            MobileMoneyPayoutEligibilityResult.Eligible());
        var fx = new RecordingFxQuoteProvider(
            new MobileMoneyPayoutFxQuote(650m, 6_500_000));
        var fees = new RecordingFeePolicy(
            new MobileMoneyPayoutFee("SERVICE_FEE", 250, "EUR"),
            new MobileMoneyPayoutFee("RAIL_FEE", 50, "EUR"));
        var orchestrator = new MobileMoneyPayoutQuoteOrchestrator(
            eligibility,
            fx,
            fees,
            clock,
            TimeSpan.FromMinutes(10));

        using var cancellation = new CancellationTokenSource();
        var response = await orchestrator.CreateAsync(
            new CreateMobileMoneyPayoutQuoteRequest(
                " de ",
                " eur ",
                " cm ",
                " xaf ",
                " mtn-cm ",
                10_000),
            cancellation.Token);

        Assert(response.QuoteId != Guid.Empty, "Quote id must be generated.");
        AssertEqual("EUR", response.SourceCurrency);
        AssertEqual(10_000L, response.SourceAmountMinor);
        AssertEqual(2, response.Fees.Count);
        AssertEqual(300L, response.TotalFeeMinor);
        AssertEqual(10_300L, response.TotalSourceDebitMinor);
        AssertEqual("XAF", response.DestinationCurrency);
        AssertEqual(6_500_000L, response.DestinationAmountMinor);
        AssertEqual(650m, response.FxRate);
        AssertEqual(clock.UtcNow, response.CreatedAtUtc);
        AssertEqual(clock.UtcNow.AddMinutes(10), response.ExpiresAtUtc);

        AssertEqual(1, eligibility.CallCount);
        AssertEqual(1, fx.CallCount);
        AssertEqual(1, fees.CallCount);
        AssertEqual(cancellation.Token, eligibility.LastCancellationToken);
        AssertEqual(cancellation.Token, fx.LastCancellationToken);
        AssertEqual(cancellation.Token, fees.LastCancellationToken);
        AssertEqual("DE", fx.LastRequest!.Corridor.SourceCountryCode);
        AssertEqual("EUR", fx.LastRequest.Corridor.SourceCurrency);
        AssertEqual("CM", fx.LastRequest.Corridor.DestinationCountryCode);
        AssertEqual("XAF", fx.LastRequest.Corridor.DestinationCurrency);
        AssertEqual("MTN-CM", fx.LastRequest.Corridor.OperatorCode);
        AssertEqual(10_000L, fx.LastRequest.SourceAmountMinor);
        AssertEqual(fx.LastRequest.Corridor, fees.LastContext!.Corridor);
        AssertEqual(10_000L, fees.LastContext.SourceAmountMinor);
    }

    public static async Task RejectsIneligibleCorridorBeforePricing()
    {
        var eligibility = new RecordingEligibilityPolicy(
            MobileMoneyPayoutEligibilityResult.Ineligible(
                MobileMoneyPayoutEligibilityCodes.CorridorDisabled));
        var fx = new RecordingFxQuoteProvider(
            new MobileMoneyPayoutFxQuote(650m, 6_500_000));
        var fees = new RecordingFeePolicy(
            new MobileMoneyPayoutFee("SERVICE_FEE", 250, "EUR"));
        var orchestrator = new MobileMoneyPayoutQuoteOrchestrator(
            eligibility,
            fx,
            fees,
            new FixedClock(Utc(2026, 10, 6, 10, 0)),
            TimeSpan.FromMinutes(10));

        var exception = await AssertThrowsAsync<MobileMoneyPayoutEligibilityException>(
            () => orchestrator.CreateAsync(ValidRequest()));

        AssertEqual(
            MobileMoneyPayoutEligibilityCodes.CorridorDisabled,
            exception.Code);
        AssertEqual(1, eligibility.CallCount);
        AssertEqual(0, fx.CallCount);
        AssertEqual(0, fees.CallCount);
    }

    public static async Task RejectsUnavailableFxBeforeFees()
    {
        var eligibility = new RecordingEligibilityPolicy(
            MobileMoneyPayoutEligibilityResult.Eligible());
        var fx = new RecordingFxQuoteProvider(null);
        var fees = new RecordingFeePolicy(
            new MobileMoneyPayoutFee("SERVICE_FEE", 250, "EUR"));
        var orchestrator = new MobileMoneyPayoutQuoteOrchestrator(
            eligibility,
            fx,
            fees,
            new FixedClock(Utc(2026, 10, 6, 10, 0)),
            TimeSpan.FromMinutes(10));

        var exception = await AssertThrowsAsync<MobileMoneyPayoutQuoteUnavailableException>(
            () => orchestrator.CreateAsync(ValidRequest()));

        AssertEqual(
            MobileMoneyPayoutQuoteUnavailableException.ErrorCode,
            exception.Code);
        AssertEqual(1, eligibility.CallCount);
        AssertEqual(1, fx.CallCount);
        AssertEqual(0, fees.CallCount);
    }

    public static Task RejectsNonPositiveLifetime()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            new MobileMoneyPayoutQuoteOrchestrator(
                new RecordingEligibilityPolicy(
                    MobileMoneyPayoutEligibilityResult.Eligible()),
                new RecordingFxQuoteProvider(
                    new MobileMoneyPayoutFxQuote(650m, 6_500_000)),
                new RecordingFeePolicy(),
                new FixedClock(Utc(2026, 10, 6, 10, 0)),
                TimeSpan.Zero));

        return Task.CompletedTask;
    }

    private static CreateMobileMoneyPayoutQuoteRequest ValidRequest() =>
        new("DE", "EUR", "CM", "XAF", "MTN-CM", 10_000);

    private static DateTimeOffset Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

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

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private static async Task<TException> AssertThrowsAsync<TException>(
        Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IMobileMoneyPayoutClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class RecordingEligibilityPolicy(
        MobileMoneyPayoutEligibilityResult result) : IMobileMoneyPayoutEligibilityPolicy
    {
        public int CallCount { get; private set; }
        public MobileMoneyPayoutCorridor? LastCorridor { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<MobileMoneyPayoutEligibilityResult> EvaluateAsync(
            MobileMoneyPayoutCorridor corridor,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCorridor = corridor;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingFxQuoteProvider(
        MobileMoneyPayoutFxQuote? quote) : IMobileMoneyPayoutFxQuoteProvider
    {
        public int CallCount { get; private set; }
        public MobileMoneyPayoutFxQuoteRequest? LastRequest { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public ValueTask<MobileMoneyPayoutFxQuote?> GetQuoteAsync(
            MobileMoneyPayoutFxQuoteRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return ValueTask.FromResult(quote);
        }
    }

    private sealed class RecordingFeePolicy(
        params MobileMoneyPayoutFee[] fees) : IMobileMoneyPayoutFeePolicy
    {
        public int CallCount { get; private set; }
        public MobileMoneyPayoutFeeContext? LastContext { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public ValueTask<IReadOnlyList<MobileMoneyPayoutFee>> CalculateAsync(
            MobileMoneyPayoutFeeContext context,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastContext = context;
            LastCancellationToken = cancellationToken;
            return ValueTask.FromResult<IReadOnlyList<MobileMoneyPayoutFee>>(
                Array.AsReadOnly(fees));
        }
    }
}
