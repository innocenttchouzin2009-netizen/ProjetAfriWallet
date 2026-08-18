namespace AfriWallet.Disputes.Investigation.Domain.Cases;

public enum InvestigationOutcome
{
    None = 0,
    EvidenceSupportsClaim = 1,
    EvidenceDoesNotSupportClaim = 2,
    InsufficientEvidence = 3,
    ManualEscalationRequired = 4
}
