using AfriWallet.Compliance.Screening.Domain.Matching;

namespace AfriWallet.Compliance.Screening.Application.Screening;

public sealed record ScreeningResult(
    Guid SubjectId,
    ScreeningDecision FinalDecision,
    IReadOnlyCollection<ScreeningMatch> Matches);