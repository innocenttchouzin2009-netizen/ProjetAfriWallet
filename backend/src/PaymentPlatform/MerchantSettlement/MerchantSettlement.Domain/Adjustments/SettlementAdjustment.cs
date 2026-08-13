namespace MerchantSettlement.Domain.Adjustments;

public sealed record SettlementAdjustment(
    Guid AdjustmentId,
    string MerchantId,
    string CurrencyCode,
    long AmountMinor,
    SettlementAdjustmentType Type,
    string Reason,
    DateTime CreatedAtUtc);

public enum SettlementAdjustmentType
{
    Credit,
    Debit
}
