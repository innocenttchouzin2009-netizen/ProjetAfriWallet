using MerchantSettlement.Domain.Profiles;

namespace MerchantSettlement.Contracts.Requests;

public sealed record CreateMerchantSettlementProfileRequest(
    string MerchantId,
    string SettlementCurrency,
    SettlementFrequency Frequency,
    int SettlementDelayDays,
    long MinimumSettlementMinor);

public sealed record CreateMerchantSettlementRequest(
    string MerchantId,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    long AdjustmentsMinor,
    long ReserveMinor,
    string IdempotencyKey);
