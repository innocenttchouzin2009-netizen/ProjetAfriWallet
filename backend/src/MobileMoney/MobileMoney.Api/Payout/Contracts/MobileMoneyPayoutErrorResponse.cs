namespace MobileMoney.Production.Payout.Contracts;

public sealed record MobileMoneyPayoutErrorResponse(
    string Code,
    string Message,
    string? CorrelationId = null);
