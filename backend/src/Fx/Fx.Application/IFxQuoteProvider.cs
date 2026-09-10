using AfriWallet.Fx.Domain;

namespace AfriWallet.Fx.Application;

public interface IFxQuoteProvider
{
    Task<FxRateQuote?> GetQuoteAsync(
        CurrencyPair pair,
        CancellationToken cancellationToken = default);
}
