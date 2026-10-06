namespace MobileMoney.Production.Payout.Quote.Abstractions;

public sealed record MobileMoneyPayoutFxQuote
{
    public MobileMoneyPayoutFxQuote(
        decimal rate,
        long destinationAmountMinor)
    {
        if (rate <= 0)
            throw new ArgumentOutOfRangeException(nameof(rate));
        if (destinationAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(destinationAmountMinor));

        Rate = rate;
        DestinationAmountMinor = destinationAmountMinor;
    }

    public decimal Rate { get; }
    public long DestinationAmountMinor { get; }
}
