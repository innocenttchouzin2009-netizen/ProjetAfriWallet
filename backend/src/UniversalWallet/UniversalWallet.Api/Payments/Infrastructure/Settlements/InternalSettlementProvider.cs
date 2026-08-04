using UniversalWallet.Api.Payments.Application.Settlements;

namespace UniversalWallet.Api.Payments.Infrastructure.Settlements;

public sealed class InternalSettlementProvider : ISettlementProvider
{
    public string Channel => "INTERNAL";

    public Task<SettlementProviderResult> SettleAsync(SettlementRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SettlementProviderResult(true, $"internal-{request.SettlementId:N}", null, null));
    }

    public Task<SettlementProviderStatus> GetStatusAsync(string providerReference, CancellationToken cancellationToken)
    {
        return Task.FromResult(SettlementProviderStatus.Settled);
    }
}
