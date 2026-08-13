using MerchantSettlement.Application.Interfaces;
using MerchantSettlement.Domain.Positions;

namespace MerchantSettlement.Infrastructure.Acquiring;

public sealed class SandboxAcquiringReadModel : IAcquiringReadModel
{
    private readonly List<MerchantSettlementTransaction> _transactions = [];

    public void Add(MerchantSettlementTransaction transaction)
    {
        _transactions.Add(transaction);
    }

    public Task<IReadOnlyCollection<MerchantSettlementTransaction>> GetCapturedTransactionsAsync(
        string merchantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var result = _transactions
            .Where(x =>
                x.MerchantId == merchantId &&
                x.CapturedAtUtc >= fromUtc &&
                x.CapturedAtUtc <= toUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<MerchantSettlementTransaction>>(result);
    }
}
