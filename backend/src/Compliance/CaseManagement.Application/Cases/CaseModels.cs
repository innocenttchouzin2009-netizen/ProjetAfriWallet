using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Application.Cases;

public sealed record ComplianceCaseResult(
    Guid CaseId,
    string Awid,
    string Title,
    ComplianceCasePriority Priority,
    ComplianceCaseStatus Status,
    ComplianceCaseDecision Decision,
    string? Assignee,
    int SourceCount,
    int NoteCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);