namespace Settlement.Domain.Positions;

public sealed record SettlementPosition(
    string CurrencyCode,
    long SettledDebitMinor,
    long SettledCreditMinor,
    long PendingDebitMinor,
    long PendingCreditMinor,
    long NetSettledMinor,
    DateTime CalculatedAtUtc);
