namespace AfriWallet.Disputes.Decision.Domain.Decisions;

public enum ResolutionReasonCode
{
    EvidenceSupportsClaim = 0,
    EvidenceDoesNotSupportClaim = 1,
    InsufficientEvidence = 2,
    InvestigationRequiresEscalation = 3,
    UnauthorizedTransaction = 10,
    DuplicateTransaction = 11,
    ProcessingError = 12,
    GoodsOrServicesNotReceived = 13,
    RefundNotProcessed = 14,
    PolicyRequiresManualReview = 20,
    HighValueRequiresApproval = 21,
    UnsupportedClassification = 22
}
