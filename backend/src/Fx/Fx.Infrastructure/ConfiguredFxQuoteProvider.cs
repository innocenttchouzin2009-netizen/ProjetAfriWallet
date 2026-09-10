using AfriWallet.Fx.Application;
using AfriWallet.Fx.Domain;

namespace AfriWallet.Fx.Infrastructure;

public sealed class ConfiguredFxQuoteProvider : IFxQuoteProvider
{
    private readonly IReadOnlyDictionary<CurrencyPair, decimal> rates;
    private readonly TimeProvider timeProvider;

    public ConfiguredFxQuoteProvider(
        IEnumerable<ConfiguredFxRate> configuredRates,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(configuredRates);

        var normalizedRates = new Dictionary<CurrencyPair, decimal>();
        foreach (var configuredRate in configuredRates)
        {
            ArgumentNullException.ThrowIfNull(configuredRate);

            var pair = new CurrencyPair(
                CurrencyCode.Create(configuredRate.SourceCurrencyCode),
                CurrencyCode.Create(configuredRate.TargetCurrencyCode));

            _ = new FxRateQuote(pair, configuredRate.Rate, DateTimeOffset.UnixEpoch);

            if (!normalizedRates.TryAdd(pair, configuredRate.Rate))
            {
                throw new InvalidOperationException(
                    $"An FX rate is already configured for {pair.BaseCurrency.Value}/{pair.QuoteCurrency.Value}.");
            }
        }

        rates = normalizedRates;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<FxRateQuote?> GetQuoteAsync(
        CurrencyPair pair,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pair);
        cancellationToken.ThrowIfCancellationRequested();

        if (!rates.TryGetValue(pair, out var rate))
        {
            return Task.FromResult<FxRateQuote?>(null);
        }

        return Task.FromResult<FxRateQuote?>(
            new FxRateQuote(pair, rate, timeProvider.GetUtcNow()));
    }
}
