using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Domain;

namespace AfriWallet.Transfer.Application;

public sealed record TransferWalletContext(
    Guid WalletId,
    AccountId AccountId,
    string CurrencyCode,
    bool IsActive,
    long AvailableMinor);

public sealed record PrepareInternalTransferCommand(
    TransferWalletContext Source,
    TransferWalletContext Target,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset RequestedAtUtc);

public sealed record InternalTransferPlan(
    TransferIntent Intent,
    JournalEntry JournalEntry);
