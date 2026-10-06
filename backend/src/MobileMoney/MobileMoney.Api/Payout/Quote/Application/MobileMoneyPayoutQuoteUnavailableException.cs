namespace MobileMoney.Production.Payout.Quote.Application;

public sealed class MobileMoneyPayoutQuoteUnavailableException : InvalidOperationException
{
    public const string ErrorCode = "PAYOUT_QUOTE_FX_UNAVAILABLE";

    public MobileMoneyPayoutQuoteUnavailableException()
        : base("No FX quote is available for the requested payout corridor.")
    {
    }

    public string Code => ErrorCode;
}
