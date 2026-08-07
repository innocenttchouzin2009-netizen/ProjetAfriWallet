namespace Treasury.Domain.Settlement;

public sealed class SettlementPosition
{
    public SettlementPosition(
        string partner,
        string currency,
        decimal netAmount)
    {
        Partner = partner;
        Currency = currency;
        NetAmount = netAmount;
    }

    public string Partner { get; }

    public string Currency { get; }

    public decimal NetAmount { get; }
}
