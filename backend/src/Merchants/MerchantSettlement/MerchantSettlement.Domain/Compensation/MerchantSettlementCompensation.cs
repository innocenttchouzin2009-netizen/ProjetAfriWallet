namespace AfriWallet.Merchants.Settlement.Domain.Compensation;
public sealed record MerchantSettlementCompensation(Guid CompensationId,string Reason,string? ProviderReference,DateTimeOffset RequestedAtUtc,DateTimeOffset? CompletedAtUtc);
