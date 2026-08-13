namespace Reconciliation.Domain.Matches;

public sealed record ReconciliationMatch(
    Guid MatchId,
    string InternalRecordId,
    string ExternalRecordId,
    ReconciliationMatchType Type,
    long AmountDifferenceMinor,
    TimeSpan TimeDifference,
    DateTime MatchedAtUtc);

public enum ReconciliationMatchType
{
    Exact,
    Partial,
    Unmatched,
    Duplicate,
    Exception
}
