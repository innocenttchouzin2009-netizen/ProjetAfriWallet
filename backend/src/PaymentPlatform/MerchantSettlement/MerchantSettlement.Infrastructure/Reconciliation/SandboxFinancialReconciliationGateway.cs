using MerchantSettlement.Application.Interfaces;
using MerchantSettlement.Domain.Reconciliation;

namespace MerchantSettlement.Infrastructure.Reconciliation;

public sealed class SandboxFinancialReconciliationGateway : IFinancialReconciliationGateway
{
    public Task<MerchantReconciliationResult> ReconcileAsync(
        Guid settlementId,
        string merchantId,
        string financialReference,
        long expectedMinor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actual = expectedMinor;

        return Task.FromResult(
            new MerchantReconciliationResult(
                settlementId,
                merchantId,
                MerchantReconciliationStatus.Matched,
                expectedMinor,
                actual,
                actual - expectedMinor,
                null,
                DateTime.UtcNow));
    }
}
