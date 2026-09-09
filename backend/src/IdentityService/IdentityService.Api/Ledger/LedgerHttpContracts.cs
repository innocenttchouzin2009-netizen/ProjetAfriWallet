using AfriWallet.Ledger.Domain;

namespace IdentityService.Api.Ledger;

public sealed record PostLedgerLineRequest(
    Guid AccountId,
    LedgerSide Side,
    long AmountMinor,
    string? Memo = null);

public sealed record PostJournalRequest(
    string CurrencyCode,
    string BusinessReference,
    Guid CorrelationId,
    IReadOnlyCollection<PostLedgerLineRequest> Lines);

public sealed record LedgerErrorResponse(
    string Code,
    string Message,
    string TraceId);
