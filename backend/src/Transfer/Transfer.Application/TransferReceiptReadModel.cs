using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Domain;

namespace AfriWallet.Transfer.Application;

public sealed record TransferReceiptReadModel
{
    public TransferId TransferId { get; }
    public JournalEntryId JournalEntryId { get; }
    public Guid SourceWalletId { get; }
    public Guid TargetWalletId { get; }
    public string CurrencyCode { get; }
    public long AmountMinor { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private TransferReceiptReadModel(
        TransferId transferId,
        JournalEntryId journalEntryId,
        Guid sourceWalletId,
        Guid targetWalletId,
        string currencyCode,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc)
    {
        TransferId = transferId;
        JournalEntryId = journalEntryId;
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        CurrencyCode = currencyCode;
        AmountMinor = amountMinor;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
    }

    public static TransferReceiptReadModel Create(
        TransferId transferId,
        JournalEntryId journalEntryId,
        Guid sourceWalletId,
        Guid targetWalletId,
        string currencyCode,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc)
    {
        if (sourceWalletId == Guid.Empty)
            throw new ArgumentException("Source wallet id cannot be empty.", nameof(sourceWalletId));
        if (targetWalletId == Guid.Empty)
            throw new ArgumentException("Target wallet id cannot be empty.", nameof(targetWalletId));
        if (sourceWalletId == targetWalletId)
            throw new InvalidOperationException("Source and target wallets must be different.");
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Transfer amount must be positive.");
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        if (createdAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Transfer timestamp must be UTC.", nameof(createdAtUtc));

        var normalizedCurrency = NormalizeCurrency(currencyCode);
        return new TransferReceiptReadModel(
            transferId,
            journalEntryId,
            sourceWalletId,
            targetWalletId,
            normalizedCurrency,
            amountMinor,
            correlationId,
            createdAtUtc);
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException("Currency code must contain exactly three ISO-like letters.", nameof(currencyCode));

        return normalized;
    }
}

public enum TransferCorrelationLookupStatus
{
    Found = 1,
    NotFound = 2
}

public sealed record TransferCorrelationLookupResult(
    TransferCorrelationLookupStatus Status,
    TransferReceiptReadModel? Receipt)
{
    public static TransferCorrelationLookupResult Found(TransferReceiptReadModel receipt) =>
        new(TransferCorrelationLookupStatus.Found, receipt);

    public static TransferCorrelationLookupResult NotFound() =>
        new(TransferCorrelationLookupStatus.NotFound, null);
}
