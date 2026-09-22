namespace AfriWallet.Merchants.Capture.Domain;

public enum MerchantCaptureStatus { Created = 0, Processing = 1, Captured = 2, Failed = 3 }

public sealed class MerchantCaptureExecution
{
    private MerchantCaptureExecution(Guid executionId, Guid decisionId, Guid paymentIntentId, string merchantId, long amountMinor, string currency, string idempotencyKey, MerchantCaptureStatus status, string? providerReference, string? failureCode, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        ExecutionId=executionId; DecisionId=decisionId; PaymentIntentId=paymentIntentId; MerchantId=merchantId; AmountMinor=amountMinor; Currency=currency; IdempotencyKey=idempotencyKey; Status=status; ProviderReference=providerReference; FailureCode=failureCode; CreatedAtUtc=createdAtUtc; UpdatedAtUtc=updatedAtUtc;
    }
    public Guid ExecutionId{get;} public Guid DecisionId{get;} public Guid PaymentIntentId{get;} public string MerchantId{get;} public long AmountMinor{get;} public string Currency{get;} public string IdempotencyKey{get;}
    public MerchantCaptureStatus Status{get;private set;} public string? ProviderReference{get;private set;} public string? FailureCode{get;private set;} public DateTimeOffset CreatedAtUtc{get;} public DateTimeOffset UpdatedAtUtc{get;private set;}
    public bool SettlementReady => Status==MerchantCaptureStatus.Captured;

    public static MerchantCaptureExecution Create(Guid decisionId,Guid paymentIntentId,string merchantId,long amountMinor,string currency,string idempotencyKey,DateTimeOffset now)
    {
        if(decisionId==Guid.Empty||paymentIntentId==Guid.Empty) throw new ArgumentException("Decision and payment intent ids are required.");
        if(string.IsNullOrWhiteSpace(merchantId)||string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Merchant id and idempotency key are required.");
        if(amountMinor<=0) throw new ArgumentOutOfRangeException(nameof(amountMinor));
        var c=NormalizeCurrency(currency); EnsureUtc(now);
        return new(Guid.NewGuid(),decisionId,paymentIntentId,merchantId.Trim(),amountMinor,c,idempotencyKey.Trim(),MerchantCaptureStatus.Created,null,null,now,now);
    }

    public static MerchantCaptureExecution Restore(Guid executionId,Guid decisionId,Guid paymentIntentId,string merchantId,long amountMinor,string currency,string idempotencyKey,MerchantCaptureStatus status,string? providerReference,string? failureCode,DateTimeOffset createdAtUtc,DateTimeOffset updatedAtUtc)
    {
        if(executionId==Guid.Empty) throw new ArgumentException("Execution id is required.",nameof(executionId));
        if(!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        var value=Create(decisionId,paymentIntentId,merchantId,amountMinor,currency,idempotencyKey,createdAtUtc);
        return new(executionId,value.DecisionId,value.PaymentIntentId,value.MerchantId,value.AmountMinor,value.Currency,value.IdempotencyKey,status,providerReference,failureCode,createdAtUtc,updatedAtUtc);
    }

    public void Start(DateTimeOffset now){EnsureUtc(now);if(Status==MerchantCaptureStatus.Captured||Status==MerchantCaptureStatus.Failed)throw new InvalidOperationException("Terminal capture execution is immutable.");Status=MerchantCaptureStatus.Processing;UpdatedAtUtc=now;}
    public void Complete(string providerReference,DateTimeOffset now){EnsureUtc(now);if(Status!=MerchantCaptureStatus.Processing)throw new InvalidOperationException("Capture execution is not processing.");if(string.IsNullOrWhiteSpace(providerReference))throw new ArgumentException("Provider reference is required.",nameof(providerReference));ProviderReference=providerReference.Trim();FailureCode=null;Status=MerchantCaptureStatus.Captured;UpdatedAtUtc=now;}
    public void Fail(string failureCode,DateTimeOffset now){EnsureUtc(now);if(Status==MerchantCaptureStatus.Captured)throw new InvalidOperationException("Captured execution is immutable.");if(string.IsNullOrWhiteSpace(failureCode))throw new ArgumentException("Failure code is required.",nameof(failureCode));FailureCode=failureCode.Trim();Status=MerchantCaptureStatus.Failed;UpdatedAtUtc=now;}
    private static string NormalizeCurrency(string v){if(string.IsNullOrWhiteSpace(v))throw new ArgumentException("Currency is required.",nameof(v));var n=v.Trim().ToUpperInvariant();if(n.Length!=3||n.Any(ch=>ch<'A'||ch>'Z'))throw new ArgumentException("Currency must contain three ISO-like letters.",nameof(v));return n;}
    private static void EnsureUtc(DateTimeOffset v){if(v.Offset!=TimeSpan.Zero)throw new ArgumentException("Timestamp must be UTC.");}
}
