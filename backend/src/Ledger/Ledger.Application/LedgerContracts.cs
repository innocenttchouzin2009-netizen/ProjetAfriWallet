using AfriWallet.Ledger.Domain;

namespace AfriWallet.Ledger.Application;

public sealed record PostLedgerLineCommand(Guid AccountId, LedgerSide Side, long AmountMinor, string? Memo = null);

public sealed record PostJournalCommand(
    string CurrencyCode,
    string BusinessReference,
    Guid CorrelationId,
    IReadOnlyCollection<PostLedgerLineCommand> Lines);

public sealed record LedgerLineView(Guid AccountId, LedgerSide Side, long AmountMinor, string? Memo);

public sealed record JournalEntryView(
    Guid JournalEntryId,
    string CurrencyCode,
    string BusinessReference,
    Guid CorrelationId,
    DateTimeOffset PostedAtUtc,
    IReadOnlyList<LedgerLineView> Lines);

public sealed record LedgerOperationResult<T>(bool Succeeded, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static LedgerOperationResult<T> Success(T value) => new(true, value, null, null);
    public static LedgerOperationResult<T> Failure(string code, string message) => new(false, default, code, message);
}

public static class LedgerErrorCode
{
    public const string ValidationError = "LEDGER_VALIDATION_ERROR";
    public const string DuplicateCorrelation = "LEDGER_DUPLICATE_CORRELATION";
    public const string NotFound = "LEDGER_NOT_FOUND";
}
