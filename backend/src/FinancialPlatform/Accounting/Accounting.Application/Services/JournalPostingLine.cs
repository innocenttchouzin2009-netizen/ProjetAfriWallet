using Accounting.Domain.Entries;

namespace Accounting.Application.Services;

public sealed record JournalPostingLine(
    Guid AccountId,
    string CurrencyCode,
    long AmountMinor,
    JournalLineSide Side,
    string? Narration);