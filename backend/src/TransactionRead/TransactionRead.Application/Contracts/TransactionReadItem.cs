using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionRead.Application.Contracts;

public sealed record TransactionReadItem(
    Guid TransactionId,
    WalletId WalletId,
    long AmountMinor,
    string CurrencyCode,
    TransactionReadDirection Direction,
    TransactionReadStatus Status,
    DateTimeOffset OccurredAtUtc,
    string Reference,
    string? CounterpartyLabel);
