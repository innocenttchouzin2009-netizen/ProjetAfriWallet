namespace Settlement.Domain.Fx;

public sealed record FxQuote(
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    DateTime QuotedAtUtc,
    DateTime ExpiresAtUtc)
{
    public long Convert(long amountMinor)
    {
        return decimal.ToInt64(decimal.Round(amountMinor * Rate, 0, MidpointRounding.AwayFromZero));
    }
}
