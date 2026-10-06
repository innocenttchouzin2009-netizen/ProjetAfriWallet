using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Abstractions;

public interface IMobileMoneyPayoutFeePolicy
{
    ValueTask<IReadOnlyList<MobileMoneyPayoutFee>> CalculateAsync(
        MobileMoneyPayoutFeeContext context,
        CancellationToken cancellationToken = default);
}
