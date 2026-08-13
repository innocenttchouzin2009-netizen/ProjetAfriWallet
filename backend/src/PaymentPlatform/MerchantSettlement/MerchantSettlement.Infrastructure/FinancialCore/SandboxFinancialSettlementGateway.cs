using MerchantSettlement.Application.Interfaces;

namespace MerchantSettlement.Infrastructure.FinancialCore;

public sealed class SandboxFinancialSettlementGateway : IFinancialSettlementGateway
{
    public Task<string> ExecuteAsync(
        Guid merchantSettlementId,
        string merchantId,
        string currencyCode,
        long netAmountMinor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (netAmountMinor <= 0)
            throw new InvalidOperationException("Settlement amount must be positive.");

        return Task.FromResult($"financial-settlement-{merchantSettlementId:N}");
    }
}
