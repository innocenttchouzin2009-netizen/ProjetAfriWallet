namespace Treasury.Domain.Positions;

public sealed record SettlementPosition(
    Guid AccountId,
    string CurrencyCode,
    long NetMinor,
    DateTime CalculatedAtUtc);
