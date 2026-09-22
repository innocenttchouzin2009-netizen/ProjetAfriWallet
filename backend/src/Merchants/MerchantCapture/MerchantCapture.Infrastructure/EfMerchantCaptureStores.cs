using System.Text.Json;
using AfriWallet.Merchants.Capture.Application;
using AfriWallet.Merchants.Capture.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Capture.Infrastructure;

public sealed class EfMerchantCaptureRepository(MerchantCaptureDbContext db):IMerchantCaptureRepository
{
    public async Task AddAsync(MerchantCaptureExecution e,CancellationToken ct=default){db.Executions.Add(Map(e));await db.SaveChangesAsync(ct);}
    public async Task SaveAsync(MerchantCaptureExecution e,CancellationToken ct=default){var row=await db.Executions.SingleAsync(x=>x.ExecutionId==e.ExecutionId,ct);Copy(e,row);await db.SaveChangesAsync(ct);}
    public async Task<MerchantCaptureExecution?> GetAsync(Guid id,CancellationToken ct=default){var x=await db.Executions.AsNoTracking().SingleOrDefaultAsync(v=>v.ExecutionId==id,ct);return x is null?null:Map(x);}
    public async Task<MerchantCaptureExecution?> GetByIdempotencyKeyAsync(string key,CancellationToken ct=default){var x=await db.Executions.AsNoTracking().SingleOrDefaultAsync(v=>v.IdempotencyKey==key,ct);return x is null?null:Map(x);}
    public async Task<MerchantCaptureExecution?> GetByDecisionAsync(Guid decisionId,CancellationToken ct=default){var x=await db.Executions.AsNoTracking().SingleOrDefaultAsync(v=>v.DecisionId==decisionId,ct);return x is null?null:Map(x);}
    private static MerchantCaptureExecutionEntity Map(MerchantCaptureExecution e){var x=new MerchantCaptureExecutionEntity();Copy(e,x);return x;}
    private static void Copy(MerchantCaptureExecution e,MerchantCaptureExecutionEntity x){x.ExecutionId=e.ExecutionId;x.DecisionId=e.DecisionId;x.PaymentIntentId=e.PaymentIntentId;x.MerchantId=e.MerchantId;x.AmountMinor=e.AmountMinor;x.Currency=e.Currency;x.IdempotencyKey=e.IdempotencyKey;x.Status=(int)e.Status;x.ProviderReference=e.ProviderReference;x.FailureCode=e.FailureCode;x.CreatedAtUtc=e.CreatedAtUtc;x.UpdatedAtUtc=e.UpdatedAtUtc;}
    private static MerchantCaptureExecution Map(MerchantCaptureExecutionEntity x)=>MerchantCaptureExecution.Restore(x.ExecutionId,x.DecisionId,x.PaymentIntentId,x.MerchantId,x.AmountMinor,x.Currency,x.IdempotencyKey,(MerchantCaptureStatus)x.Status,x.ProviderReference,x.FailureCode,x.CreatedAtUtc,x.UpdatedAtUtc);
}

public sealed class EfMerchantCaptureAuditStore(MerchantCaptureDbContext db):IMerchantCaptureAuditStore
{
    public async Task AppendAsync(MerchantCaptureAuditEvent e,CancellationToken ct=default){db.Audit.Add(new MerchantCaptureAuditEntity{EventId=e.EventId,ExecutionId=e.ExecutionId,DecisionId=e.DecisionId,PaymentIntentId=e.PaymentIntentId,MerchantId=e.MerchantId,EventType=e.EventType,Actor=e.Actor,OccurredAtUtc=e.OccurredAtUtc,MetadataJson=JsonSerializer.Serialize(e.Metadata)});await db.SaveChangesAsync(ct);}
    public async Task<IReadOnlyCollection<MerchantCaptureAuditEvent>> ListAsync(Guid id,CancellationToken ct=default)
    {
        var rows=await db.Audit.AsNoTracking().Where(x=>x.ExecutionId==id).ToListAsync(ct);
        return rows.OrderBy(x=>x.OccurredAtUtc).ThenBy(x=>x.EventId).Select(x=>new MerchantCaptureAuditEvent(x.EventId,x.ExecutionId,x.DecisionId,x.PaymentIntentId,x.MerchantId,x.EventType,x.Actor,x.OccurredAtUtc,JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson)??new())).ToArray();
    }
}
