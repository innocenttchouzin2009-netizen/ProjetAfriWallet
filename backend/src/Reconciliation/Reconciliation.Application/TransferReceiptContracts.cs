using AfriWallet.Ledger.Domain;

namespace AfriWallet.Reconciliation.Application;

public enum TransferReceiptLookupStatus
{
    Found = 1,
    NotFound = 2,
    NotTransferJournal = 3,
    InvalidTransferJournal = 4
}

public sealed record TransferReceipt(
    Guid TransferId,
    JournalEntryId JournalEntryId,
    Guid CorrelationId,
    string CurrencyCode,
    long AmountMinor,
    AccountId DebitAccountId,
    AccountId CreditAccountId,
    DateTimeOffset PostedAtUtc,
    string BusinessReference);

public sealed record TransferReceiptLookupResult(
    TransferReceiptLookupStatus Status,
    TransferReceipt? Receipt,
    string? Reason)
{
    public static TransferReceiptLookupResult Found(TransferReceipt receipt) =>
        new(TransferReceiptLookupStatus.Found, receipt, null);

    public static TransferReceiptLookupResult NotFound() =>
        new(TransferReceiptLookupStatus.NotFound, null, null);

    public static TransferReceiptLookupResult NotTransferJournal() =>
        new(TransferReceiptLookupStatus.NotTransferJournal, null, null);

    public static TransferReceiptLookupResult Invalid(string reason) =>
        new(TransferReceiptLookupStatus.InvalidTransferJournal, null, reason);
}
