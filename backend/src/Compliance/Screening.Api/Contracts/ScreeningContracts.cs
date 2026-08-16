using AfriWallet.Compliance.Screening.Domain.Subjects;

namespace AfriWallet.Compliance.Screening.Api.Contracts;

public sealed record ScreenSubjectRequest(
    Guid SubjectId,
    ScreeningSubjectType Type,
    string FullName,
    DateOnly? DateOfBirth,
    string? CountryCode,
    string? ExternalReference);