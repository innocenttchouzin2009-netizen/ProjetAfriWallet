namespace MerchantSettlement.Domain.Positions;

public sealed record MerchantSettlementTransaction(
    Guid PaymentId,
    string MerchantId,
    string CurrencyCode,
    long GrossAmountMinor,
    long FeeMinor,
    long RefundedMinor,
    DateTime CapturedAtUtc)
{
    public long NetMinor => GrossAmountMinor - FeeMinor - RefundedMinor;
}
