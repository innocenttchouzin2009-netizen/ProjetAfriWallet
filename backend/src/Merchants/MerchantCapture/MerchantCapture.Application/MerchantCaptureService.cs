using AfriWallet.Merchants.Capture.Domain;

namespace AfriWallet.Merchants.Capture.Application;

public sealed record ExecuteMerchantCaptureCommand(Guid DecisionId,string IdempotencyKey,string Actor);
public sealed record MerchantCaptureResult(Guid ExecutionId,Guid DecisionId,Guid PaymentIntentId,string MerchantId,long AmountMinor,string Currency,MerchantCaptureStatus Status,string? ProviderReference,string? FailureCode,bool SettlementReady,string IdempotencyKey,DateTimeOffset CreatedAtUtc,DateTimeOffset UpdatedAtUtc);

public sealed class MerchantCaptureService(IMerchantCaptureRepository repository,ICaptureEligibleDecisionReader decisions,IMerchantCaptureProvider provider,IMerchantCaptureAuditStore audit,TimeProvider timeProvider)
{
    public async Task<MerchantCaptureResult> ExecuteAsync(ExecuteMerchantCaptureCommand command,CancellationToken cancellationToken=default)
    {
        if(command.DecisionId==Guid.Empty)throw new ArgumentException("Decision id is required.",nameof(command));
        if(string.IsNullOrWhiteSpace(command.IdempotencyKey)||string.IsNullOrWhiteSpace(command.Actor))throw new ArgumentException("Idempotency key and actor are required.",nameof(command));

        var existing=await repository.GetByIdempotencyKeyAsync(command.IdempotencyKey,cancellationToken)??await repository.GetByDecisionAsync(command.DecisionId,cancellationToken);
        if(existing is not null)
        {
            if(existing.DecisionId!=command.DecisionId)throw new InvalidOperationException("Idempotency key belongs to a different capture decision.");
            if(existing.Status is MerchantCaptureStatus.Captured or MerchantCaptureStatus.Failed)return Map(existing);
            return await SubmitAsync(existing,command.Actor,cancellationToken);
        }

        var decision=await decisions.GetAsync(command.DecisionId,cancellationToken)??throw new KeyNotFoundException("Capture-eligible decision not found.");
        EnsureEligible(decision);
        var execution=MerchantCaptureExecution.Create(decision.DecisionId,decision.PaymentIntentId,decision.MerchantId,decision.AmountMinor,decision.Currency,command.IdempotencyKey,timeProvider.GetUtcNow());
        await repository.AddAsync(execution,cancellationToken);
        await WriteAudit(execution,"capture.created",command.Actor,cancellationToken);
        return await SubmitAsync(execution,command.Actor,cancellationToken);
    }

    public async Task<MerchantCaptureResult> GetAsync(Guid executionId,CancellationToken cancellationToken=default)=>Map(await repository.GetAsync(executionId,cancellationToken)??throw new KeyNotFoundException("Merchant capture execution not found."));

    private async Task<MerchantCaptureResult> SubmitAsync(MerchantCaptureExecution execution,string actor,CancellationToken ct)
    {
        execution.Start(timeProvider.GetUtcNow());
        await repository.SaveAsync(execution,ct);
        var result=await provider.CaptureAsync(new(execution.ExecutionId,execution.DecisionId,execution.PaymentIntentId,execution.MerchantId,execution.AmountMinor,execution.Currency,execution.IdempotencyKey),ct);
        if(result.Succeeded)
        {
            execution.Complete(result.ProviderReference??throw new InvalidOperationException("Capture provider reference is required."),timeProvider.GetUtcNow());
            await repository.SaveAsync(execution,ct);
            await WriteAudit(execution,"capture.completed",actor,ct);
        }
        else
        {
            execution.Fail(result.FailureCode??"provider_failure",timeProvider.GetUtcNow());
            await repository.SaveAsync(execution,ct);
            await WriteAudit(execution,"capture.failed",actor,ct);
        }
        return Map(execution);
    }

    private static void EnsureEligible(CaptureEligibleDecisionSnapshot d)
    {
        if(!string.Equals(d.DecisionType,"CaptureEligible",StringComparison.OrdinalIgnoreCase)||!string.Equals(d.DecisionStatus,"Approved",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Decision must be CaptureEligible and Approved.");
        if(!string.Equals(d.MerchantRegistryStatus,"Active",StringComparison.OrdinalIgnoreCase)||!string.Equals(d.MerchantVerificationStatus,"Verified",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Merchant must be active and verified.");
        if(d.AmountMinor<=0)throw new InvalidOperationException("Capture amount must be positive.");
        if(string.IsNullOrWhiteSpace(d.Currency)||d.Currency.Trim().Length!=3)throw new InvalidOperationException("Capture currency is invalid.");
    }

    private Task WriteAudit(MerchantCaptureExecution e,string type,string actor,CancellationToken ct)=>audit.AppendAsync(new(Guid.NewGuid(),e.ExecutionId,e.DecisionId,e.PaymentIntentId,e.MerchantId,type,actor,timeProvider.GetUtcNow(),new Dictionary<string,string>{{"status",e.Status.ToString()},{"captureExecutionPerformed",(e.Status==MerchantCaptureStatus.Captured).ToString().ToLowerInvariant()},{"settlementPerformed","false"},{"payoutPerformed","false"},{"ledgerMutationPerformed","false"},{"settlementReady",e.SettlementReady.ToString().ToLowerInvariant()}}),ct);
    private static MerchantCaptureResult Map(MerchantCaptureExecution e)=>new(e.ExecutionId,e.DecisionId,e.PaymentIntentId,e.MerchantId,e.AmountMinor,e.Currency,e.Status,e.ProviderReference,e.FailureCode,e.SettlementReady,e.IdempotencyKey,e.CreatedAtUtc,e.UpdatedAtUtc);
}
