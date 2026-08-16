using AfriWallet.Compliance.CaseManagement.Domain.Cases;

namespace AfriWallet.Compliance.CaseManagement.Domain.Sources;

public sealed record CaseSourceReference(
    Guid Id,
    CaseSourceType Type,
    string SourceId,
    string Summary,
    DateTimeOffset LinkedAtUtc);