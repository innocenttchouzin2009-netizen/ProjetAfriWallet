using Treasury.Domain.Accounts;

namespace Treasury.Contracts.Requests;

public sealed record CreateTreasuryAccountRequest(
    string AccountCode,
    string DisplayName,
    string CurrencyCode,
    TreasuryAccountType Type);

public sealed record PostTreasuryTransactionRequest(
    string Reference,
    string CorrelationId,
    Guid DebitAccountId,
    Guid CreditAccountId,
    string CurrencyCode,
    long AmountMinor);

public sealed record CreateTreasuryReservationRequest(
    long AmountMinor,
    string Reference);
