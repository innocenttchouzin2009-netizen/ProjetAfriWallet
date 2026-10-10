using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed record ExecuteMobileMoneyPayoutFundingCommand(
    Guid CorrelationId);

public sealed record MobileMoneyPayoutFundingExecutionResult(
    Guid CorrelationId,
    IReadOnlyList<FundingAttempt> Attempts);
