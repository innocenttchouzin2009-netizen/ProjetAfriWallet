using AfriWallet.Transfer.Domain;
using AfriWallet.Ledger.Domain;

namespace AfriWallet.Transfer.Application;

public sealed record ExecuteInternalTransferCommand(
    Guid SourceWalletId,
    Guid TargetWalletId,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset RequestedAtUtc);

public sealed record InternalTransferExecutionResult(
    TransferIntent Intent,
    JournalEntry JournalEntry);
