namespace MerchantSettlement.Domain.Positions;

public sealed record MerchantSettlementPosition(
    string MerchantId,
    string CurrencyCode,
    long GrossMinor,
    long FeesMinor,
    long RefundsMinor,
    long AdjustmentsMinor,
    long ReserveMinor,
    long NetPayableMinor,
    DateTime CalculatedAtUtc);
