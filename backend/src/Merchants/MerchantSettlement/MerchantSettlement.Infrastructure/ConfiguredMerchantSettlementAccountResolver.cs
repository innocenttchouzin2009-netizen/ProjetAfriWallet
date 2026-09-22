using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Domain.Settlements;

namespace AfriWallet.Merchants.Settlement.Infrastructure;

public sealed class ConfiguredMerchantSettlementAccountResolver(
    IReadOnlyDictionary<string, MerchantSettlementAccountRoute> routes)
    : IMerchantSettlementAccountResolver
{
    public Task<MerchantSettlementAccountRoute?> ResolveAsync(
        string merchantId,
        MerchantSettlementRoute route,
        string currency,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = $"{merchantId.Trim()}:{route}:{currency.Trim().ToUpperInvariant()}";
        routes.TryGetValue(key, out var result);
        return Task.FromResult(result);
    }

    public static string Key(string merchantId, MerchantSettlementRoute route, string currency) =>
        $"{merchantId.Trim()}:{route}:{currency.Trim().ToUpperInvariant()}";
}
