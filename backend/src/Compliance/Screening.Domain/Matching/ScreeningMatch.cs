using AfriWallet.Compliance.Screening.Domain.Entries;

namespace AfriWallet.Compliance.Screening.Domain.Matching;

public sealed record ScreeningMatch(
    Guid MatchId,
    Guid SubjectId,
    Guid EntryId,
    ScreeningEntryType EntryType,
    string SourceCode,
    double Score,
    ScreeningDecision Decision,
    IReadOnlyCollection<string> Reasons,
    DateTimeOffset CreatedAtUtc);