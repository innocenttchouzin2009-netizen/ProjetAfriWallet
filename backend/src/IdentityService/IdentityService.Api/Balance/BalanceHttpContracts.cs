namespace IdentityService.Api.Balance;

public sealed record BalanceResponse(
    Guid AccountId,
    string CurrencyCode,
    long DebitMinor,
    long CreditMinor,
    long NetMinor);

public sealed record BalanceErrorResponse(
    string Code,
    string Message,
    string TraceId);
