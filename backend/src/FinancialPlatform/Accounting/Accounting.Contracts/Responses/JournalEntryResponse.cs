namespace Accounting.Contracts.Responses;

public sealed record JournalEntryResponse(
    Guid JournalEntryId,
    Guid PeriodId,
    string Reference,
    string Description,
    Guid? SourceJournalEntryId,
    DateTime PostedAtUtc,
    IReadOnlyCollection<JournalEntryLineResponse> Entries);

public sealed record JournalEntryLineResponse(
    Guid EntryId,
    Guid AccountId,
    string CurrencyCode,
    long DebitMinor,
    long CreditMinor,
    string? Narration,
    DateTime PostedAtUtc);