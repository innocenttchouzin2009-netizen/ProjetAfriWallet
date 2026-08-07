using Settlement.Application.Interfaces;
using Settlement.Domain.Fx;

namespace Settlement.Infrastructure.Providers;

public sealed class SandboxFxQuoteProvider : IFxQuoteProvider
{
    private static readonly Dictionary<string, decimal> Rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["XAF:USD"] = 0.0016m,
        ["USD:XAF"] = 625m,
        ["XAF:EUR"] = 0.0015m,
        ["EUR:XAF"] = 666.6667m,
        ["USD:EUR"] = 0.92m,
        ["EUR:USD"] = 1.087m
    };

    public Task<FxQuote> GetQuoteAsync(
        string baseCurrency,
        string quoteCurrency,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Amount must be greater than zero.");
        }

        if (string.Equals(baseCurrency, quoteCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new FxQuote(
                baseCurrency.ToUpperInvariant(),
                quoteCurrency.ToUpperInvariant(),
                1m,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(5)));
        }

        var key = $"{baseCurrency.ToUpperInvariant()}:{quoteCurrency.ToUpperInvariant()}";
        if (!Rates.TryGetValue(key, out var rate))
        {
            throw new InvalidOperationException($"No sandbox FX quote available for {key}.");
        }

        return Task.FromResult(new FxQuote(
            baseCurrency.ToUpperInvariant(),
            quoteCurrency.ToUpperInvariant(),
            rate,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5)));
    }
}
