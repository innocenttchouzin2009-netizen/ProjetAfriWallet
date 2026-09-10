using AfriWallet.Fx.Application;
using AfriWallet.Fx.Domain;

Run("currency codes normalize", () =>
{
    var currency = CurrencyCode.Create(" xaf ");
    Assert(currency.Value == "XAF", "Currency must normalize to uppercase ASCII code.");
});

Run("same-currency pairs are rejected", () =>
{
    AssertThrows<ArgumentException>(() => new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("eur")));
});

Run("non-positive rates are rejected", () =>
{
    var pair = new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("XAF"));
    AssertThrows<ArgumentOutOfRangeException>(() => new FxRateQuote(pair, 0m, DateTimeOffset.UnixEpoch));
});

Run("rate precision above twelve fractional digits is rejected", () =>
{
    var pair = new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("XAF"));
    AssertThrows<ArgumentOutOfRangeException>(() => new FxRateQuote(pair, 1.1234567890123m, DateTimeOffset.UnixEpoch));
});

Run("quote timestamp must be UTC", () =>
{
    var pair = new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("XAF"));
    var nonUtc = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(2));
    AssertThrows<ArgumentException>(() => new FxRateQuote(pair, 655.957m, nonUtc));
});

Run("conversion respects source and target minor units", () =>
{
    var service = new FxConversionService();
    var result = service.Convert(new FxConversionCommand("eur", "xaf", 10_000, 2, 0, 655.957m, DateTimeOffset.UnixEpoch));
    Assert(result.SourceCurrencyCode == "EUR" && result.TargetCurrencyCode == "XAF", "Currencies must normalize.");
    Assert(result.TargetAmountMinor == 65_596, $"Expected 65596, got {result.TargetAmountMinor}.");
});

Run("midpoint rounding uses bankers rounding", () =>
{
    var service = new FxConversionService();
    var result = service.Convert(new FxConversionCommand("USD", "EUR", 1, 0, 0, 2.5m, DateTimeOffset.UnixEpoch));
    Assert(result.TargetAmountMinor == 2, $"Expected ToEven rounding to 2, got {result.TargetAmountMinor}.");
    Assert(result.RoundingMode == "ToEven", "Rounding mode must be explicit in the result.");
});

Run("zero converts deterministically to zero", () =>
{
    var service = new FxConversionService();
    var result = service.Convert(new FxConversionCommand("GBP", "NGN", 0, 2, 2, 2100.25m, DateTimeOffset.UnixEpoch));
    Assert(result.TargetAmountMinor == 0, "Zero source amount must convert to zero.");
});

Run("negative amounts are rejected", () =>
{
    var service = new FxConversionService();
    AssertThrows<ArgumentOutOfRangeException>(() => service.Convert(new FxConversionCommand("EUR", "USD", -1, 2, 2, 1.1m, DateTimeOffset.UnixEpoch)));
});

Run("unsupported minor-unit precision is rejected", () =>
{
    var service = new FxConversionService();
    AssertThrows<ArgumentOutOfRangeException>(() => service.Convert(new FxConversionCommand("EUR", "USD", 100, 7, 2, 1.1m, DateTimeOffset.UnixEpoch)));
});

Console.WriteLine("FX Domain/Application scenarios passed.");

static void Run(string name, Action scenario)
{
    scenario();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
