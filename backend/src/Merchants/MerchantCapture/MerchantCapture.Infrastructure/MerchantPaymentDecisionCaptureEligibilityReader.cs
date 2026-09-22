using AfriWallet.Merchants.Capture.Application;
using AfriWallet.Merchants.PaymentDecision.Application.Abstractions;
using AfriWallet.Merchants.PaymentDecision.Domain.Decisions;

namespace AfriWallet.Merchants.Capture.Infrastructure;

public sealed class MerchantPaymentDecisionCaptureEligibilityReader(IMerchantPaymentDecisionRepository decisions,IPaymentIntentDecisionReader intents):ICaptureEligibleDecisionReader
{
    public async Task<CaptureEligibleDecisionSnapshot?> GetAsync(Guid decisionId,CancellationToken ct=default)
    {
        var d=await decisions.GetAsync(decisionId,ct);
        if(d is null)return null;
        var i=await intents.GetAsync(d.PaymentIntentId,ct)??throw new InvalidOperationException("Payment intent backing the decision is unavailable.");
        if(i.PaymentIntentId!=d.PaymentIntentId||!string.Equals(i.MerchantId,d.MerchantId,StringComparison.Ordinal))throw new InvalidOperationException("Payment decision and intent are inconsistent.");
        return new(d.DecisionId,d.PaymentIntentId,d.MerchantId,d.DecisionType.ToString(),d.Status.ToString(),i.AmountMinor,i.Currency,i.MerchantRegistryStatus,i.MerchantVerificationStatus);
    }
}

public sealed class IdempotentSandboxMerchantCaptureProvider:IMerchantCaptureProvider
{
    private readonly Dictionary<string,MerchantCaptureProviderResult> _results=new(StringComparer.Ordinal);
    public int Calls{get;private set;}
    public Task<MerchantCaptureProviderResult> CaptureAsync(MerchantCaptureProviderRequest request,CancellationToken ct=default)
    {
        ct.ThrowIfCancellationRequested();
        if(_results.TryGetValue(request.IdempotencyKey,out var existing))return Task.FromResult(existing);
        Calls++;
        var result=new MerchantCaptureProviderResult(true,$"SANDBOX-CAP-{request.ExecutionId:N}",null);
        _results[request.IdempotencyKey]=result;
        return Task.FromResult(result);
    }
}
