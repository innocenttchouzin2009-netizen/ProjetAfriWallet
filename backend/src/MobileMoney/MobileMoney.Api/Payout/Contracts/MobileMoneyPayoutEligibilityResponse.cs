namespace MobileMoney.Production.Payout.Contracts;

public sealed record MobileMoneyPayoutEligibilityResponse(
    bool IsEligible,
    string? FailureCode);
