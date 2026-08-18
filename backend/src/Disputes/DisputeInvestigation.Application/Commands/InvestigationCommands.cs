using AfriWallet.Disputes.Investigation.Domain.Cases;
using AfriWallet.Disputes.Investigation.Domain.Evidence;

namespace AfriWallet.Disputes.Investigation.Application.Commands;

public sealed record CreateInvestigationCommand(Guid ClaimId, string Actor);
public sealed record AssignInvestigationCommand(Guid InvestigationId, string AnalystId, string Actor);
public sealed record RequestEvidenceCommand(Guid InvestigationId, EvidenceType Type, string RequestedFrom, string Reason, string Actor);
public sealed record AddEvidenceCommand(
    Guid InvestigationId,
    EvidenceType Type,
    string Reference,
    string Description,
    string Sha256,
    long SizeBytes,
    string ContentType,
    string SubmittedBy,
    string Actor);
public sealed record CompleteInvestigationCommand(Guid InvestigationId, InvestigationOutcome Outcome, string Actor);
