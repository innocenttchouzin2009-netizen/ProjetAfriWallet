namespace AfriWallet.Fx.Domain;

public sealed record CurrencyPair
{
    public CurrencyCode BaseCurrency { get; }
    public CurrencyCode QuoteCurrency { get; }

    public CurrencyPair(CurrencyCode baseCurrency, CurrencyCode quoteCurrency)
    {
        ArgumentNullException.ThrowIfNull(baseCurrency);
        ArgumentNullException.ThrowIfNull(quoteCurrency);

        if (baseCurrency == quoteCurrency)
        {
            throw new ArgumentException("FX currency pair must contain two different currencies.");
        }

        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
    }

    public override string ToString() => $"{BaseCurrency}/{QuoteCurrency}";
}
