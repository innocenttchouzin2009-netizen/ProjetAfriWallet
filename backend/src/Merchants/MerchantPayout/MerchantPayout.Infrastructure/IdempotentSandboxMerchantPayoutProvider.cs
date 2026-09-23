using AfriWallet.Merchants.Payout.Application;

namespace AfriWallet.Merchants.Payout.Infrastructure;

public sealed class IdempotentSandboxMerchantPayoutProvider : IMerchantPayoutProvider
{
    private readonly Dictionary<string, MerchantPayoutProviderResult> results = new(StringComparer.Ordinal);

    public int Calls { get; private set; }

    public Task<MerchantPayoutProviderResult> ExecuteAsync(
        MerchantPayoutProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (results.TryGetValue(request.IdempotencyKey, out var existing))
            return Task.FromResult(existing);

        Calls++;
        var result = new MerchantPayoutProviderResult(
            true,
            $"SANDBOX-PAYOUT-{request.PayoutId:N}",
            null);
        results.Add(request.IdempotencyKey, result);
        return Task.FromResult(result);
    }
}
