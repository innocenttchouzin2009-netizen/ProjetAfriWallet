namespace AfriWallet.Compliance.CaseManagement.Domain.Notes;

public sealed record ComplianceCaseNote(
    Guid Id,
    string Author,
    string Content,
    DateTimeOffset CreatedAtUtc);