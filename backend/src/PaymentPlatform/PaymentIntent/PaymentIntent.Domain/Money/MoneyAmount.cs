namespace PaymentIntent.Domain.Money;

public sealed record MoneyAmount
{
    public MoneyAmount(
        long amountMinor,
        string currencyCode)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amountMinor),
                "Payment amount must be greater than zero.");

        var currency =
            currencyCode.Trim().ToUpperInvariant();

        if (currency.Length != 3)
            throw new ArgumentException(
                "Currency must use ISO 4217 format.");

        AmountMinor = amountMinor;
        CurrencyCode = currency;
    }

    public long AmountMinor { get; }

    public string CurrencyCode { get; }
}
