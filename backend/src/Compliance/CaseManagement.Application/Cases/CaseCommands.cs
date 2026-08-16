using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Application.Cases;

public sealed record CreateCaseCommand(string Awid, string Title, ComplianceCasePriority Priority, string Actor);
public sealed record AddCaseSourceCommand(Guid CaseId, CaseSourceType SourceType, string SourceId, string Summary, string Actor);
public sealed record AssignCaseCommand(Guid CaseId, string Assignee, string Actor);
public sealed record AddCaseNoteCommand(Guid CaseId, string Content, string Actor);
public sealed record EscalateCaseCommand(Guid CaseId, ComplianceCasePriority Priority, string Actor);
public sealed record ResolveCaseCommand(Guid CaseId, ComplianceCaseDecision Decision, string Actor);