namespace AfriWallet.Compliance.Screening.Domain.Subjects;

public sealed record ScreeningSubject(
    Guid SubjectId,
    ScreeningSubjectType Type,
    string FullName,
    DateOnly? DateOfBirth,
    string? CountryCode,
    string? ExternalReference);