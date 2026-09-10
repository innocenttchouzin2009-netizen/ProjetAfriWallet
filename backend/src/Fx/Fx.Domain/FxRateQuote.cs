namespace AfriWallet.Fx.Domain;

public sealed record FxRateQuote
{
    public const int MaximumRateScale = 12;

    public CurrencyPair Pair { get; }
    public decimal Rate { get; }
    public DateTimeOffset QuotedAtUtc { get; }

    public FxRateQuote(CurrencyPair pair, decimal rate, DateTimeOffset quotedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(pair);

        if (rate <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "FX rate must be greater than zero.");
        }

        if (GetScale(rate) > MaximumRateScale)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), $"FX rate supports at most {MaximumRateScale} fractional digits.");
        }

        if (quotedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("FX quote timestamp must be UTC.", nameof(quotedAtUtc));
        }

        Pair = pair;
        Rate = rate;
        QuotedAtUtc = quotedAtUtc;
    }

    private static int GetScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;
}
