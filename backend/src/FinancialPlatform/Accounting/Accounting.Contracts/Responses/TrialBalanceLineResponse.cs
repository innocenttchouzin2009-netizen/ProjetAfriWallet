namespace Accounting.Contracts.Responses;

public sealed record TrialBalanceLineResponse(
    Guid AccountId,
    string AccountCode,
    string DisplayName,
    string CurrencyCode,
    long DebitMinor,
    long CreditMinor,
    long NetMinor);