using MerchantSettlement.Application.Interfaces;
using MerchantSettlement.Domain.Positions;

namespace MerchantSettlement.Application.Services;

public sealed class MerchantSettlementPositionService
{
    private readonly IAcquiringReadModel _acquiring;

    public MerchantSettlementPositionService(IAcquiringReadModel acquiring)
    {
        _acquiring = acquiring;
    }

    public async Task<MerchantSettlementPosition> CalculateAsync(
        string merchantId,
        string currencyCode,
        DateTime fromUtc,
        DateTime toUtc,
        long adjustmentsMinor,
        long reserveMinor,
        CancellationToken cancellationToken)
    {
        var transactions = await _acquiring.GetCapturedTransactionsAsync(
            merchantId,
            fromUtc,
            toUtc,
            cancellationToken);

        var eligible = transactions
            .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var gross = eligible.Sum(x => x.GrossAmountMinor);
        var fees = eligible.Sum(x => x.FeeMinor);
        var refunds = eligible.Sum(x => x.RefundedMinor);
        var net = checked(gross - fees - refunds + adjustmentsMinor - reserveMinor);

        return new MerchantSettlementPosition(
            merchantId,
            currencyCode.ToUpperInvariant(),
            gross,
            fees,
            refunds,
            adjustmentsMinor,
            reserveMinor,
            net,
            DateTime.UtcNow);
    }
}
