using AfriWallet.Fx.Application;
using AfriWallet.Fx.Domain;
using AfriWallet.Fx.Infrastructure;

await RunAsync("configured rate returns concrete quote", async () =>
{
    var now = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
    var provider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)],
        new FixedTimeProvider(now));

    var quote = await provider.GetQuoteAsync(new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("XAF")));
    Assert(quote is not null, "Expected configured quote.");
    Assert(quote!.Rate == 655.957m, "Configured rate must be preserved.");
    Assert(quote.QuotedAtUtc == now, "Quote timestamp must come from TimeProvider.");
});

await RunAsync("configured currencies normalize", async () =>
{
    var provider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate(" eur ", " xaf ", 655.957m)],
        new FixedTimeProvider(DateTimeOffset.UnixEpoch));

    var quote = await provider.GetQuoteAsync(new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("XAF")));
    Assert(quote is not null, "Normalized configured pair must be retrievable.");
});

await RunAsync("unknown pair returns unavailable", async () =>
{
    var provider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)]);

    var quote = await provider.GetQuoteAsync(new CurrencyPair(CurrencyCode.Create("USD"), CurrencyCode.Create("XAF")));
    Assert(quote is null, "Unknown pair must return null.");
});

await RunAsync("reverse pair is not inferred", async () =>
{
    var provider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)]);

    var quote = await provider.GetQuoteAsync(new CurrencyPair(CurrencyCode.Create("XAF"), CurrencyCode.Create("EUR")));
    Assert(quote is null, "Provider must not infer inverse rates implicitly.");
});

Run("duplicate configured pairs are rejected", () =>
{
    AssertThrows<InvalidOperationException>(() => new ConfiguredFxQuoteProvider(
    [
        new ConfiguredFxRate("EUR", "XAF", 655.957m),
        new ConfiguredFxRate(" eur ", "xaf", 655.957m)
    ]));
});

Run("invalid configured rates are rejected", () =>
{
    AssertThrows<ArgumentOutOfRangeException>(() => new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 0m)]));
});

await RunAsync("cancellation is propagated", async () =>
{
    var provider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)]);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    await AssertThrowsAsync<OperationCanceledException>(() => provider.GetQuoteAsync(
        new CurrencyPair(CurrencyCode.Create("EUR"), CurrencyCode.Create("XAF")),
        cancellation.Token));
});

await RunAsync("quote application service consumes concrete adapter", async () =>
{
    var provider = new ConfiguredFxQuoteProvider(
        [new ConfiguredFxRate("EUR", "XAF", 655.957m)],
        new FixedTimeProvider(DateTimeOffset.UnixEpoch));
    var service = new FxQuoteApplicationService(provider);

    var result = await service.GetQuoteAsync(new FxQuoteRequest("eur", "xaf"));
    Assert(result.Succeeded, "Application service must succeed with concrete adapter.");
    Assert(result.Value?.Rate == 655.957m, "Application service must expose configured rate.");
});

Console.WriteLine("FX Infrastructure scenarios passed.");

static void Run(string name, Action scenario)
{
    scenario();
    Console.WriteLine($"PASS: {name}");
}

static async Task RunAsync(string name, Func<Task> scenario)
{
    await scenario();
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

static async Task AssertThrowsAsync<TException>(Func<Task> action) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
