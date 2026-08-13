using Accounting.Domain.Entries;

namespace Accounting.Contracts.Requests;

public sealed record PostJournalEntryRequest(
    Guid PeriodId,
    string Reference,
    string Description,
    IReadOnlyCollection<PostJournalEntryLineRequest> Lines);

public sealed record PostJournalEntryLineRequest(
    Guid AccountId,
    string CurrencyCode,
    long AmountMinor,
    JournalLineSide Side,
    string? Narration);