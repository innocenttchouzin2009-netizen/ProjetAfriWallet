using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;

namespace MobileMoney.Production.Payout.Execution.Infrastructure;

public sealed class StoreBackedMobileMoneyPayoutExecutionQuoteBindingReader
    : IMobileMoneyPayoutExecutionQuoteBindingReader
{
    private readonly IMobileMoneyPayoutQuoteStore _quoteStore;

    public StoreBackedMobileMoneyPayoutExecutionQuoteBindingReader(
        IMobileMoneyPayoutQuoteStore quoteStore)
    {
        _quoteStore = quoteStore ??
            throw new ArgumentNullException(nameof(quoteStore));
    }

    public async Task<MobileMoneyPayoutExecutionQuoteBinding?> FindByIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default)
    {
        var quote = await _quoteStore.FindByIdAsync(
            quoteId,
            cancellationToken);

        if (quote is null)
            return null;

        return MobileMoneyPayoutExecutionQuoteBinding.Create(
            quote.QuoteId,
            quote.Corridor.SourceCountryCode,
            quote.Corridor.SourceCurrency,
            quote.SourceAmountMinor,
            quote.TotalFeeMinor,
            quote.TotalSourceDebitMinor,
            quote.Corridor.DestinationCountryCode,
            quote.Corridor.DestinationCurrency,
            quote.DestinationAmountMinor,
            quote.Corridor.OperatorCode,
            quote.ExpiresAtUtc);
    }
}
