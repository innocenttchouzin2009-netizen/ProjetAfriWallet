using AfriWallet.Compliance.CaseManagement.Domain.Cases;
namespace AfriWallet.Compliance.CaseManagement.Api.Contracts;
public sealed record CreateCaseRequest(string Awid, string Title, ComplianceCasePriority Priority);
public sealed record AddCaseSourceRequest(CaseSourceType SourceType, string SourceId, string Summary);
public sealed record AssignCaseRequest(string Assignee);
public sealed record AddCaseNoteRequest(string Content);
public sealed record EscalateCaseRequest(ComplianceCasePriority Priority);
public sealed record ResolveCaseRequest(ComplianceCaseDecision Decision);