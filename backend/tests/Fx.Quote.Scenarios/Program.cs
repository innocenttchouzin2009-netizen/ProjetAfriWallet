using AfriWallet.Fx.Application;
using AfriWallet.Fx.Domain;

await RunAsync("quote request normalizes pair and returns provider quote", async () =>
{
    var quotedAt = DateTimeOffset.UnixEpoch;
    var provider = new RecordingQuoteProvider(pair =>
        new FxRateQuote(pair, 655.957m, quotedAt));
    var service = new FxQuoteApplicationService(provider);

    var result = await service.GetQuoteAsync(new FxQuoteRequest(" eur ", "xaf"));

    Assert(result.Succeeded, "Quote request should succeed.");
    Assert(result.Value is not null, "Successful quote must contain a value.");
    Assert(result.Value!.SourceCurrencyCode == "EUR", "Source currency must normalize.");
    Assert(result.Value.TargetCurrencyCode == "XAF", "Target currency must normalize.");
    Assert(result.Value.Rate == 655.957m, "Provider rate must be preserved.");
    Assert(provider.LastPair?.BaseCurrency.Value == "EUR" && provider.LastPair?.QuoteCurrency.Value == "XAF", "Normalized pair must be sent to provider.");
});

await RunAsync("invalid pair returns validation failure without calling provider", async () =>
{
    var provider = new RecordingQuoteProvider(_ => throw new InvalidOperationException("Provider must not be called."));
    var service = new FxQuoteApplicationService(provider);

    var result = await service.GetQuoteAsync(new FxQuoteRequest("EUR", "eur"));

    Assert(!result.Succeeded, "Same-currency pair must fail.");
    Assert(result.ErrorCode == FxQuoteErrorCode.ValidationError, "Expected validation error code.");
    Assert(provider.CallCount == 0, "Provider must not be called for invalid pair.");
});

await RunAsync("unavailable provider quote maps to explicit failure", async () =>
{
    var provider = new RecordingQuoteProvider(_ => null);
    var service = new FxQuoteApplicationService(provider);

    var result = await service.GetQuoteAsync(new FxQuoteRequest("USD", "NGN"));

    Assert(!result.Succeeded, "Missing quote must fail.");
    Assert(result.ErrorCode == FxQuoteErrorCode.QuoteUnavailable, "Expected quote unavailable error code.");
});

await RunAsync("provider pair mismatch is rejected", async () =>
{
    var provider = new RecordingQuoteProvider(_ =>
        new FxRateQuote(
            new CurrencyPair(CurrencyCode.Create("GBP"), CurrencyCode.Create("XAF")),
            800m,
            DateTimeOffset.UnixEpoch));
    var service = new FxQuoteApplicationService(provider);

    var result = await service.GetQuoteAsync(new FxQuoteRequest("EUR", "XAF"));

    Assert(!result.Succeeded, "Mismatched provider quote must fail.");
    Assert(result.ErrorCode == FxQuoteErrorCode.ProviderQuoteMismatch, "Expected provider mismatch error code.");
});

await RunAsync("cancellation token is propagated to provider", async () =>
{
    using var source = new CancellationTokenSource();
    var provider = new RecordingQuoteProvider(pair =>
        new FxRateQuote(pair, 1.1m, DateTimeOffset.UnixEpoch));
    var service = new FxQuoteApplicationService(provider);

    await service.GetQuoteAsync(new FxQuoteRequest("EUR", "USD"), source.Token);

    Assert(provider.LastCancellationToken == source.Token, "Cancellation token must be propagated.");
});

await RunAsync("provider cancellation remains observable", async () =>
{
    using var source = new CancellationTokenSource();
    source.Cancel();
    var provider = new CancellingQuoteProvider();
    var service = new FxQuoteApplicationService(provider);

    await AssertThrowsAsync<OperationCanceledException>(() =>
        service.GetQuoteAsync(new FxQuoteRequest("EUR", "USD"), source.Token));
});

Console.WriteLine("FX Quote provider/application scenarios passed.");

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

sealed class RecordingQuoteProvider(Func<CurrencyPair, FxRateQuote?> quoteFactory) : IFxQuoteProvider
{
    public CurrencyPair? LastPair { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }
    public int CallCount { get; private set; }

    public Task<FxRateQuote?> GetQuoteAsync(CurrencyPair pair, CancellationToken cancellationToken = default)
    {
        LastPair = pair;
        LastCancellationToken = cancellationToken;
        CallCount++;
        return Task.FromResult(quoteFactory(pair));
    }
}

sealed class CancellingQuoteProvider : IFxQuoteProvider
{
    public Task<FxRateQuote?> GetQuoteAsync(CurrencyPair pair, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<FxRateQuote?>(null);
    }
}
