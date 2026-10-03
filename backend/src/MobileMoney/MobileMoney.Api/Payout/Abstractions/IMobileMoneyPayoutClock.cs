namespace MobileMoney.Production.Payout.Abstractions;

public interface IMobileMoneyPayoutClock
{
    DateTimeOffset UtcNow { get; }
}
