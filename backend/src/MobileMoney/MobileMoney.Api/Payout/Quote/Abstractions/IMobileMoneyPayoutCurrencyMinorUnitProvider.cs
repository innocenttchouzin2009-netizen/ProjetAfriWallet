namespace MobileMoney.Production.Payout.Quote.Abstractions;

public interface IMobileMoneyPayoutCurrencyMinorUnitProvider
{
    byte GetMinorUnitDigits(string currencyCode);
}
