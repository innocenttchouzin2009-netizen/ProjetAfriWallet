using MobileMoney.Production.Payout.Abstractions;

namespace MobileMoney.Production.Payout.Quote.Infrastructure;

public sealed class SystemMobileMoneyPayoutClock(TimeProvider timeProvider)
    : IMobileMoneyPayoutClock
{
    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
}
