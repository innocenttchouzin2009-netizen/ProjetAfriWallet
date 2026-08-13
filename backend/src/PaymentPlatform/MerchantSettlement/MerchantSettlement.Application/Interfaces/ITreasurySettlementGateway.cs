using MerchantSettlement.Domain.Positions;

namespace MerchantSettlement.Application.Interfaces;

public interface IAcquiringReadModel
{
    Task<IReadOnlyCollection<MerchantSettlementTransaction>> GetCapturedTransactionsAsync(
        string merchantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}

public interface IFinancialSettlementGateway
{
    Task<string> ExecuteAsync(
        Guid merchantSettlementId,
        string merchantId,
        string currencyCode,
        long netAmountMinor,
        CancellationToken cancellationToken);
}

public interface IFinancialReconciliationGateway
{
    Task<MerchantSettlement.Domain.Reconciliation.MerchantReconciliationResult> ReconcileAsync(
        Guid settlementId,
        string merchantId,
        string financialReference,
        long expectedMinor,
        CancellationToken cancellationToken);
}
