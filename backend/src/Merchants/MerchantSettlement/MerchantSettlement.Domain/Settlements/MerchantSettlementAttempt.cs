namespace AfriWallet.Merchants.Settlement.Domain.Settlements;
public sealed record MerchantSettlementAttempt(Guid AttemptId,int AttemptNumber,string CorrelationId,string? ProviderReference,string Result,DateTimeOffset AttemptedAtUtc);
