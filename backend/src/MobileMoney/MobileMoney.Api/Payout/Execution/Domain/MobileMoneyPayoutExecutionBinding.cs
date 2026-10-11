namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionBinding
{
    private MobileMoneyPayoutExecutionBinding(
        MobileMoneyPayoutExecutionQuote quote,
        MobileMoneyPayoutExecutionIntent intent)
    {
        Quote = quote;
        Intent = intent;
    }

    public MobileMoneyPayoutExecutionQuote Quote { get; }
    public MobileMoneyPayoutExecutionIntent Intent { get; }

    public static MobileMoneyPayoutExecutionBinding Create(
        MobileMoneyPayoutExecutionQuote quote,
        MobileMoneyPayoutExecutionIntent intent,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(quote);
        ArgumentNullException.ThrowIfNull(intent);

        if (nowUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", nameof(nowUtc));

        if (quote.QuoteId != intent.QuoteId)
        {
            throw new MobileMoneyPayoutExecutionException(
                MobileMoneyPayoutExecutionErrorCodes.QuoteBindingMismatch);
        }

        if (nowUtc >= quote.ExpiresAtUtc)
        {
            throw new MobileMoneyPayoutExecutionException(
                MobileMoneyPayoutExecutionErrorCodes.QuoteExpired);
        }

        if (!string.Equals(
                quote.Corridor.DestinationCountryCode,
                intent.Beneficiary.CountryCode,
                StringComparison.Ordinal)
            || !string.Equals(
                quote.Corridor.OperatorCode,
                intent.Beneficiary.OperatorCode,
                StringComparison.Ordinal))
        {
            throw new MobileMoneyPayoutExecutionException(
                MobileMoneyPayoutExecutionErrorCodes.QuoteBindingMismatch);
        }

        return new(quote, intent);
    }
}
