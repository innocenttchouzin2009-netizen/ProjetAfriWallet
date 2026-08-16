using AfriWallet.Compliance.Screening.Domain.Subjects;

namespace AfriWallet.Compliance.Screening.Application.Screening;

public sealed record ScreenSubjectCommand(
    ScreeningSubject Subject,
    string Actor);