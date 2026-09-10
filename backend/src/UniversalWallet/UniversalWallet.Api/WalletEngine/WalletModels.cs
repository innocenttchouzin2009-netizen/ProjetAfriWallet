namespace UniversalWallet.Api.WalletEngine;

public enum WalletType
{
    Personal,
    Business,
    Association,
    Savings,
    VirtualCard,
    Escrow
}

public enum WalletStatus
{
    Created,
    Active,
    Suspended,
    Closed
}

public enum LedgerEntryType
{
    Debit,
    Credit
}

public enum IntentType
{
    PaymentIntent,
    TransferIntent,
    DepositIntent,
    WithdrawalIntent,
    ExchangeIntent
}

public sealed class Wallet
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AwidId { get; init; }
    public string WalletNumber { get; init; } = string.Empty;
    public WalletType WalletType { get; init; }
    public string Currency { get; init; } = string.Empty;
    public WalletStatus Status { get; set; } = WalletStatus.Created;
    public decimal AvailableBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LedgerEntry
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid WalletId { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public LedgerEntryType EntryType { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal BalanceAfter { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class FinancialIntent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public IntentType Type { get; init; }
    public string Awid { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
