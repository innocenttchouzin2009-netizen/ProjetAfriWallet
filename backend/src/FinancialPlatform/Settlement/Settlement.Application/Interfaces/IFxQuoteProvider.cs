using Settlement.Domain.Fx;

namespace Settlement.Application.Interfaces;

public interface IFxQuoteProvider
{
    Task<FxQuote> GetQuoteAsync(
        string baseCurrency,
        string quoteCurrency,
        long amountMinor,
        CancellationToken cancellationToken);
}
