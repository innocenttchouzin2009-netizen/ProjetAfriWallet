namespace Accounting.Domain.TrialBalance;

public sealed record TrialBalanceLine(
    Guid AccountId,
    string AccountCode,
    string DisplayName,
    string CurrencyCode,
    long DebitMinor,
    long CreditMinor,
    long NetMinor);