using AfriWallet.Merchants.Onboarding.Application.Abstractions;

namespace AfriWallet.Merchants.Onboarding.Infrastructure;

public sealed class SandboxMerchantVerificationProvider : IMerchantVerificationProvider
{
    private readonly Queue<VerificationProviderDecision> _decisions = new();

    public void Enqueue(VerificationProviderDecision decision) => _decisions.Enqueue(decision);

    public Task<MerchantVerificationProviderResult> VerifyAsync(MerchantVerificationProviderRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var decision = _decisions.Count > 0 ? _decisions.Dequeue() : VerificationProviderDecision.Verified;
        return Task.FromResult(
            new MerchantVerificationProviderResult(decision, $"Sandbox verification result: {decision}", $"SANDBOX-KYB-{request.VerificationId:N}"));
    }
}
