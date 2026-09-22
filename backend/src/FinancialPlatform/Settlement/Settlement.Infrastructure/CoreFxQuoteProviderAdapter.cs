using AfriWallet.Fx.Application;
using Settlement.Application.Interfaces;
using Settlement.Domain.Fx;

namespace Settlement.Infrastructure.Providers;

public sealed class CoreFxQuoteProviderAdapter(FxQuoteApplicationService fxQuoteService) : Settlement.Application.Interfaces.IFxQuoteProvider
{
    public async Task<FxQuote> GetQuoteAsync(
        string sourceCurrency,
        string destinationCurrency,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        var result = await fxQuoteService.GetQuoteAsync(
            new FxQuoteRequest(sourceCurrency, destinationCurrency),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
            throw new InvalidOperationException(result.ErrorMessage ?? "FX quote unavailable.");

        var value = result.Value;
        return new FxQuote(
            value.SourceCurrencyCode,
            value.TargetCurrencyCode,
            value.Rate,
            value.QuotedAtUtc.UtcDateTime,
            value.QuotedAtUtc.AddMinutes(5).UtcDateTime);
    }
}
