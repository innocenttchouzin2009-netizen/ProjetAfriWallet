using AfriWallet.Disputes.Eligibility.Domain.Claims;
using AfriWallet.Disputes.Eligibility.Domain.Classification;

namespace AfriWallet.Disputes.Eligibility.Application.Policies;

public sealed class DisputeClassificationPolicy
{
    public DisputeClassification Classify(DisputeClaimType type) => type switch
    {
        DisputeClaimType.TransactionNotRecognized =>
            new(DisputeCategory.UnauthorizedTransaction, "Customer does not recognize transaction."),
        DisputeClaimType.DuplicateCharge or DisputeClaimType.WrongAmount =>
            new(DisputeCategory.ProcessingError, "Transaction processing discrepancy."),
        DisputeClaimType.ServiceNotReceived or DisputeClaimType.GoodsNotReceived or DisputeClaimType.MerchantDispute =>
            new(DisputeCategory.MerchantService, "Merchant/service fulfillment dispute."),
        DisputeClaimType.RefundNotReceived =>
            new(DisputeCategory.RefundIssue, "Expected refund was not received."),
        DisputeClaimType.CashWithdrawalDispute =>
            new(DisputeCategory.CashWithdrawal, "Cash withdrawal dispute."),
        DisputeClaimType.BankTransferDispute =>
            new(DisputeCategory.BankTransfer, "Bank transfer dispute."),
        DisputeClaimType.FraudRelated =>
            new(DisputeCategory.FraudRelated, "Claim contains fraud-related context."),
        _ =>
            new(DisputeCategory.Other, "No specific category matched.")
    };
}
