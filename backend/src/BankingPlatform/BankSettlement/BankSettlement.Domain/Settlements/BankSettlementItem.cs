namespace AfriWallet.BankingPlatform.BankSettlement.Domain.Settlements;

public sealed class BankSettlementItem
{
    public BankSettlementItem(
        Guid settlementItemId,
        Guid executionId,
        string providerCode,
        string railCode,
        long amountMinor,
        long feeMinor,
        string currencyCode,
        string providerReference)
    {
        if (settlementItemId == Guid.Empty)
            throw new ArgumentException("Settlement item ID is required.");

        if (executionId == Guid.Empty)
            throw new ArgumentException("Execution ID is required.");

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        if (feeMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(feeMinor));

        SettlementItemId = settlementItemId;
        ExecutionId = executionId;
        ProviderCode = Require(providerCode);
        RailCode = Require(railCode);
        AmountMinor = amountMinor;
        FeeMinor = feeMinor;
        CurrencyCode = NormalizeCurrency(currencyCode);
        ProviderReference = Require(providerReference);
    }

    public Guid SettlementItemId { get; }

    public Guid ExecutionId { get; }

    public string ProviderCode { get; }

    public string RailCode { get; }

    public long AmountMinor { get; }

    public long FeeMinor { get; }

    public long NetAmountMinor =>
        checked(AmountMinor - FeeMinor);

    public string CurrencyCode { get; }

    public string ProviderReference { get; }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }

    private static string NormalizeCurrency(string value)
    {
        var currency = Require(value).ToUpperInvariant();

        if (currency.Length != 3)
            throw new ArgumentException(
                "Currency must use ISO 4217 format.");

        return currency;
    }
}
