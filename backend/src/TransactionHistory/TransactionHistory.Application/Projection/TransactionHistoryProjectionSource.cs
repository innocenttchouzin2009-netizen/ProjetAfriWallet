using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Application.Projection;

public sealed record TransactionHistoryProjectionSource(
    Guid TransactionId,
    WalletId WalletId,
    long AmountMinor,
    string CurrencyCode,
    LedgerSide WalletLedgerSide,
    TransactionHistoryStatus Status,
    DateTimeOffset OccurredAtUtc,
    string Reference,
    string? CounterpartyLabel);
