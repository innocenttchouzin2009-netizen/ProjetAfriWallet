namespace Treasury.Domain.Balances;

public sealed record TreasuryBalance(
    Guid AccountId,
    string CurrencyCode,
    long DebitMinor,
    long CreditMinor,
    long NetMinor,
    DateTime CalculatedAtUtc);
