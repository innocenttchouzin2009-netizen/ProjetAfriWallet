using AfriWallet.Fx.Application;
using AfriWallet.Fx.Domain;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Infrastructure;

internal static class CoreFxAdapterScenarios
{
    public static async Task ConvertsEurCentsToXafUnits()
    {
        var pair = Pair("EUR", "XAF");
        var provider = new RecordingCoreFxQuoteProvider(
            new FxRateQuote(pair, 655.957m, UtcNow()));
        var adapter = CreateAdapter(
            provider,
            ("EUR", (byte)2),
            ("XAF", (byte)0));

        using var cancellation = new CancellationTokenSource();
        var quote = await adapter.GetQuoteAsync(
            Request("DE", "EUR", "CM", "XAF", 10_000),
            cancellation.Token);

        Assert(quote is not null, "FX quote must be returned.");
        AssertEqual(655.957m, quote!.Rate);
        AssertEqual(65_596L, quote.DestinationAmountMinor);
        AssertEqual(cancellation.Token, provider.LastCancellationToken);
    }

    public static async Task ConvertsFractionalTargetMinorUnits()
    {
        var pair = Pair("EUR", "USD");
        var adapter = CreateAdapter(
            new RecordingCoreFxQuoteProvider(
                new FxRateQuote(pair, 1.1m, UtcNow())),
            ("EUR", (byte)2),
            ("USD", (byte)2));

        var quote = await adapter.GetQuoteAsync(
            Request("DE", "EUR", "US", "USD", 12_345));

        Assert(quote is not null, "FX quote must be returned.");
        AssertEqual(13_580L, quote!.DestinationAmountMinor);
    }

    public static async Task ReturnsNullWhenCoreQuoteUnavailable()
    {
        var adapter = CreateAdapter(
            new RecordingCoreFxQuoteProvider(null),
            ("EUR", (byte)2),
            ("XAF", (byte)0));

        var quote = await adapter.GetQuoteAsync(
            Request("DE", "EUR", "CM", "XAF", 10_000));

        Assert(quote is null, "Unavailable core FX quote must remain unavailable.");
    }

    public static async Task RejectsUnknownMinorUnitCurrency()
    {
        var pair = Pair("EUR", "XAF");
        var adapter = CreateAdapter(
            new RecordingCoreFxQuoteProvider(
                new FxRateQuote(pair, 655.957m, UtcNow())),
            ("EUR", (byte)2));

        await AssertThrowsAsync<KeyNotFoundException>(() =>
            adapter.GetQuoteAsync(
                    Request("DE", "EUR", "CM", "XAF", 10_000))
                .AsTask());
    }

    public static async Task RejectsMismatchedCorePair()
    {
        var requested = Pair("EUR", "XAF");
        var mismatched = Pair("USD", "XAF");
        var adapter = CreateAdapter(
            new RecordingCoreFxQuoteProvider(
                new FxRateQuote(mismatched, 655.957m, UtcNow())),
            ("EUR", (byte)2),
            ("XAF", (byte)0));

        await AssertThrowsAsync<InvalidOperationException>(() =>
            adapter.GetQuoteAsync(
                    Request("DE", "EUR", "CM", "XAF", 10_000))
                .AsTask());
    }

    private static CoreFxMobileMoneyPayoutFxQuoteProvider CreateAdapter(
        IFxQuoteProvider provider,
        params (string CurrencyCode, byte Digits)[] minorUnits) =>
        new(
            provider,
            new FxConversionService(),
            new ConfiguredMobileMoneyPayoutCurrencyMinorUnitProvider(
                minorUnits.Select(item =>
                    new KeyValuePair<string, byte>(
                        item.CurrencyCode,
                        item.Digits))));

    private static MobileMoneyPayoutFxQuoteRequest Request(
        string sourceCountry,
        string sourceCurrency,
        string destinationCountry,
        string destinationCurrency,
        long sourceAmountMinor) =>
        new(
            new MobileMoneyPayoutCorridor(
                sourceCountry,
                sourceCurrency,
                destinationCountry,
                destinationCurrency,
                "MTN-CM"),
            sourceAmountMinor);

    private static CurrencyPair Pair(string source, string target) =>
        new(CurrencyCode.Create(source), CurrencyCode.Create(target));

    private static DateTimeOffset UtcNow() =>
        new(2026, 10, 6, 21, 30, 0, TimeSpan.Zero);

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

    private static async Task AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private sealed class RecordingCoreFxQuoteProvider(FxRateQuote? quote)
        : IFxQuoteProvider
    {
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<FxRateQuote?> GetQuoteAsync(
            CurrencyPair pair,
            CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(quote);
        }
    }
}
