using AfriWallet.Disputes.Investigation.Domain.Cases;
using AfriWallet.Disputes.Investigation.Domain.Evidence;

namespace AfriWallet.Disputes.Investigation.Api.Contracts;

public sealed record CreateInvestigationRequest(Guid ClaimId);
public sealed record AssignInvestigationRequest(string AnalystId);
public sealed record RequestEvidenceRequest(EvidenceType Type, string RequestedFrom, string Reason);
public sealed record AddEvidenceRequest(
    EvidenceType Type,
    string Reference,
    string Description,
    string Sha256,
    long SizeBytes,
    string ContentType,
    string SubmittedBy);
public sealed record CompleteInvestigationRequest(InvestigationOutcome Outcome);
