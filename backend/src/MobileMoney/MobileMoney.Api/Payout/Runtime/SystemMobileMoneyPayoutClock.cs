using MobileMoney.Production.Payout.Abstractions;

namespace MobileMoney.Production.Payout.Runtime;

public sealed class SystemMobileMoneyPayoutClock : IMobileMoneyPayoutClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
