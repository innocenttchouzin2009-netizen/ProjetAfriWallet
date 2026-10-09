using MobileMoney.Production.Payout.Contracts;

namespace MobileMoney.Production.Payout.Execution.Contracts;

public sealed record ExecuteMobileMoneyPayoutRequest(
    Guid QuoteId,
    string SourceWalletId,
    MobileMoneyBeneficiaryRequest Beneficiary,
    string IdempotencyKey);
