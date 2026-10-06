using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Domain;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("fee normalizes code and currency", FeeNormalizes),
    ("fee rejects invalid values", FeeRejectsInvalidValues),
    ("quote computes fees and total debit", QuoteComputesTotals),
    ("quote rejects invalid monetary values", QuoteRejectsInvalidMonetaryValues),
    ("quote requires UTC timestamps and positive lifetime", QuoteRequiresUtcLifetime),
    ("quote rejects fee currency mismatch", QuoteRejectsFeeCurrencyMismatch),
    ("quote rejects null fee entries", QuoteRejectsNullFeeEntries),
    ("quote detects total debit overflow", QuoteDetectsOverflow),
    ("fee policy contract receives corridor and source amount", FeePolicyContract),
    ("runtime fee policy selects amount tiers at boundaries", RuntimeFeePolicyScenarios.SelectsTierAtBoundaries),
    ("runtime fee policy supports explicit zero-fee corridors", RuntimeFeePolicyScenarios.SupportsExplicitZeroFeeTier),
    ("runtime fee policy fails closed for unconfigured corridors", RuntimeFeePolicyScenarios.FailsClosedWhenCorridorIsUnconfigured),
    ("runtime fee policy fails closed for amount gaps", RuntimeFeePolicyScenarios.FailsClosedWhenAmountFallsInGap),
    ("runtime fee policy rejects overlapping tiers", RuntimeFeePolicyScenarios.RejectsOverlappingTiers),
    ("runtime fee policy rejects fee currency mismatch", RuntimeFeePolicyScenarios.RejectsFeeCurrencyMismatch),
    ("runtime fee policy rejects duplicate fee codes", RuntimeFeePolicyScenarios.RejectsDuplicateFeeCodes),
    ("runtime fee policy honors cancellation", RuntimeFeePolicyScenarios.HonorsCancellation),
    ("core fx adapter converts EUR cents to XAF units", CoreFxAdapterScenarios.ConvertsEurCentsToXafUnits),
    ("core fx adapter preserves fractional target minor units", CoreFxAdapterScenarios.ConvertsFractionalTargetMinorUnits),
    ("core fx adapter returns unavailable quote", CoreFxAdapterScenarios.ReturnsNullWhenCoreQuoteUnavailable),
    ("core fx adapter fails closed for unknown minor-unit metadata", CoreFxAdapterScenarios.RejectsUnknownMinorUnitCurrency),
    ("core fx adapter rejects provider pair mismatch", CoreFxAdapterScenarios.RejectsMismatchedCorePair),
    ("quote orchestrator composes eligibility fx fees and domain", OrchestrationScenarios.ComposesQuote),
    ("quote orchestrator rejects ineligible corridor before pricing", OrchestrationScenarios.RejectsIneligibleCorridorBeforePricing),
    ("quote orchestrator rejects unavailable fx before fees", OrchestrationScenarios.RejectsUnavailableFxBeforeFees),
    ("quote orchestrator rejects non-positive lifetime", OrchestrationScenarios.RejectsNonPositiveLifetime),
    ("quote API route is stable", ApiScenarios.RouteIsStable),
    ("quote API returns orchestrated quote", ApiScenarios.ReturnsQuote),
    ("quote API maps invalid request", ApiScenarios.MapsInvalidRequest),
    ("quote API maps ineligible corridor", ApiScenarios.MapsIneligibleCorridor),
    ("quote API maps unavailable pricing", ApiScenarios.MapsUnavailablePricing)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"MobileMoney payout quote domain/fee-policy/orchestration scenarios: {scenarios.Length}/{scenarios.Length} passed.");

static Task FeeNormalizes()
{
    var fee = new MobileMoneyPayoutFee(" service_fee ", 250, " eur ");

    AssertEqual("SERVICE_FEE", fee.Code);
    AssertEqual(250L, fee.AmountMinor);
    AssertEqual("EUR", fee.Currency);

    return Task.CompletedTask;
}

static Task FeeRejectsInvalidValues()
{
    AssertThrows<ArgumentException>(() =>
        new MobileMoneyPayoutFee("   ", 100, "EUR"));

    AssertThrows<ArgumentOutOfRangeException>(() =>
        new MobileMoneyPayoutFee("SERVICE_FEE", -1, "EUR"));

    AssertThrows<ArgumentException>(() =>
        new MobileMoneyPayoutFee("SERVICE_FEE", 100, "EU"));

    AssertThrows<ArgumentException>(() =>
        new MobileMoneyPayoutFee("SERVICE_FEE", 100, "E1R"));

    return Task.CompletedTask;
}

static Task QuoteComputesTotals()
{
    var createdAt = Utc(2026, 10, 6, 10, 0);
    var fees = new[]
    {
        new MobileMoneyPayoutFee("SERVICE_FEE", 250, "EUR"),
        new MobileMoneyPayoutFee("RAIL_FEE", 50, "EUR")
    };

    var quote = MobileMoneyPayoutQuote.Create(
        ValidCorridor(),
        10_000,
        6_500_000,
        650m,
        fees,
        createdAt,
        createdAt.AddMinutes(10));

    Assert(quote.QuoteId != Guid.Empty, "Quote id must be generated.");
    AssertEqual(10_000L, quote.SourceAmountMinor);
    AssertEqual(6_500_000L, quote.DestinationAmountMinor);
    AssertEqual(650m, quote.FxRate);
    AssertEqual(300L, quote.TotalFeeMinor);
    AssertEqual(10_300L, quote.TotalSourceDebitMinor);
    AssertEqual(createdAt, quote.CreatedAtUtc);
    AssertEqual(createdAt.AddMinutes(10), quote.ExpiresAtUtc);
    AssertEqual(2, quote.Fees.Count);
    AssertEqual("SERVICE_FEE", quote.Fees[0].Code);
    AssertEqual("RAIL_FEE", quote.Fees[1].Code);

    fees[0] = new MobileMoneyPayoutFee("MUTATED", 999, "EUR");
    AssertEqual("SERVICE_FEE", quote.Fees[0].Code);

    return Task.CompletedTask;
}

static Task QuoteRejectsInvalidMonetaryValues()
{
    var now = Utc(2026, 10, 6, 10, 0);
    var corridor = ValidCorridor();
    var fees = Array.Empty<MobileMoneyPayoutFee>();

    AssertThrows<ArgumentOutOfRangeException>(() =>
        MobileMoneyPayoutQuote.Create(corridor, 0, 1, 1m, fees, now, now.AddMinutes(1)));

    AssertThrows<ArgumentOutOfRangeException>(() =>
        MobileMoneyPayoutQuote.Create(corridor, 1, 0, 1m, fees, now, now.AddMinutes(1)));

    AssertThrows<ArgumentOutOfRangeException>(() =>
        MobileMoneyPayoutQuote.Create(corridor, 1, 1, 0m, fees, now, now.AddMinutes(1)));

    return Task.CompletedTask;
}

static Task QuoteRequiresUtcLifetime()
{
    var utc = Utc(2026, 10, 6, 10, 0);
    var nonUtc = utc.ToOffset(TimeSpan.FromHours(2));

    AssertThrows<ArgumentException>(() =>
        MobileMoneyPayoutQuote.Create(
            ValidCorridor(),
            1_000,
            650_000,
            650m,
            Array.Empty<MobileMoneyPayoutFee>(),
            nonUtc,
            utc.AddMinutes(5)));

    AssertThrows<ArgumentException>(() =>
        MobileMoneyPayoutQuote.Create(
            ValidCorridor(),
            1_000,
            650_000,
            650m,
            Array.Empty<MobileMoneyPayoutFee>(),
            utc,
            utc));

    return Task.CompletedTask;
}

static Task QuoteRejectsFeeCurrencyMismatch()
{
    var now = Utc(2026, 10, 6, 10, 0);

    AssertThrows<ArgumentException>(() =>
        MobileMoneyPayoutQuote.Create(
            ValidCorridor(),
            10_000,
            6_500_000,
            650m,
            new[] { new MobileMoneyPayoutFee("SERVICE_FEE", 100, "USD") },
            now,
            now.AddMinutes(10)));

    return Task.CompletedTask;
}

static Task QuoteRejectsNullFeeEntries()
{
    var now = Utc(2026, 10, 6, 10, 0);
    var fees = new MobileMoneyPayoutFee[]
    {
        new("SERVICE_FEE", 100, "EUR"),
        null!
    };

    AssertThrows<ArgumentException>(() =>
        MobileMoneyPayoutQuote.Create(
            ValidCorridor(),
            10_000,
            6_500_000,
            650m,
            fees,
            now,
            now.AddMinutes(10)));

    return Task.CompletedTask;
}

static Task QuoteDetectsOverflow()
{
    var now = Utc(2026, 10, 6, 10, 0);

    AssertThrows<OverflowException>(() =>
        MobileMoneyPayoutQuote.Create(
            ValidCorridor(),
            long.MaxValue,
            1,
            1m,
            new[] { new MobileMoneyPayoutFee("SERVICE_FEE", 1, "EUR") },
            now,
            now.AddMinutes(10)));

    return Task.CompletedTask;
}

static async Task FeePolicyContract()
{
    var corridor = ValidCorridor();
    var policy = new RecordingFeePolicy(
        new MobileMoneyPayoutFee("SERVICE_FEE", 125, "EUR"));

    using var cancellation = new CancellationTokenSource();
    var fees = await policy.CalculateAsync(
        new MobileMoneyPayoutFeeContext(corridor, 5_000),
        cancellation.Token);

    AssertEqual(1, policy.CallCount);
    Assert(policy.LastContext is not null, "Fee policy must receive a context.");
    AssertEqual(corridor, policy.LastContext!.Corridor);
    AssertEqual(5_000L, policy.LastContext.SourceAmountMinor);
    AssertEqual(cancellation.Token, policy.LastCancellationToken);
    AssertEqual(1, fees.Count);
    AssertEqual("SERVICE_FEE", fees[0].Code);
    AssertEqual(125L, fees[0].AmountMinor);
    AssertEqual("EUR", fees[0].Currency);
}

static MobileMoneyPayoutCorridor ValidCorridor() =>
    new("DE", "EUR", "CM", "XAF", "MTN-CM");

static DateTimeOffset Utc(
    int year,
    int month,
    int day,
    int hour,
    int minute) =>
    new(year, month, day, hour, minute, 0, TimeSpan.Zero);

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException(
            $"Expected '{expected}', got '{actual}'.");
}

static void AssertThrows<TException>(Action action)
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

file sealed class RecordingFeePolicy(
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
