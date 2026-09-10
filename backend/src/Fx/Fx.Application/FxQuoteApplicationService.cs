using AfriWallet.Fx.Domain;

namespace AfriWallet.Fx.Application;

public sealed class FxQuoteApplicationService(IFxQuoteProvider quoteProvider)
{
    public async Task<FxQuoteOperationResult> GetQuoteAsync(
        FxQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CurrencyPair pair;
        try
        {
            pair = new CurrencyPair(
                CurrencyCode.Create(request.SourceCurrencyCode),
                CurrencyCode.Create(request.TargetCurrencyCode));
        }
        catch (ArgumentException exception)
        {
            return FxQuoteOperationResult.Failure(
                FxQuoteErrorCode.ValidationError,
                exception.Message);
        }

        var quote = await quoteProvider.GetQuoteAsync(pair, cancellationToken);
        if (quote is null)
        {
            return FxQuoteOperationResult.Failure(
                FxQuoteErrorCode.QuoteUnavailable,
                $"No FX quote is available for {pair.BaseCurrency.Value}/{pair.QuoteCurrency.Value}.");
        }

        if (quote.Pair != pair)
        {
            return FxQuoteOperationResult.Failure(
                FxQuoteErrorCode.ProviderQuoteMismatch,
                "FX provider returned a quote for a different currency pair.");
        }

        return FxQuoteOperationResult.Success(new FxQuoteView(
            quote.Pair.BaseCurrency.Value,
            quote.Pair.QuoteCurrency.Value,
            quote.Rate,
            quote.QuotedAtUtc));
    }
}
