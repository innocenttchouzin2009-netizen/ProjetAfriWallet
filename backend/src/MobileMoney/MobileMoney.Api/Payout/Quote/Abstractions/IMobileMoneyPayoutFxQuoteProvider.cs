namespace MobileMoney.Production.Payout.Quote.Abstractions;

public interface IMobileMoneyPayoutFxQuoteProvider
{
    ValueTask<MobileMoneyPayoutFxQuote?> GetQuoteAsync(
        MobileMoneyPayoutFxQuoteRequest request,
        CancellationToken cancellationToken = default);
}
