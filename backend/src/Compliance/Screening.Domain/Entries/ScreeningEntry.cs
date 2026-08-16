namespace AfriWallet.Compliance.Screening.Domain.Entries;

public sealed record ScreeningEntry(
    Guid EntryId,
    ScreeningEntryType Type,
    string PrimaryName,
    IReadOnlyCollection<string> Aliases,
    DateOnly? DateOfBirth,
    string? CountryCode,
    ScreeningSource Source);