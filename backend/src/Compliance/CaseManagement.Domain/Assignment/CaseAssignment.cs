namespace AfriWallet.Compliance.CaseManagement.Domain.Assignment;

public sealed record CaseAssignment(
    string Assignee,
    string AssignedBy,
    DateTimeOffset AssignedAtUtc);