namespace AfriWallet.Transfer.Domain;

public sealed record TransferIntent
{
    public TransferId Id { get; }
    public Guid SourceWalletId { get; }
    public Guid TargetWalletId { get; }
    public string CurrencyCode { get; }
    public long AmountMinor { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private TransferIntent(
        TransferId id,
        Guid sourceWalletId,
        Guid targetWalletId,
        string currencyCode,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        CurrencyCode = currencyCode;
        AmountMinor = amountMinor;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
    }

    public static TransferIntent Create(
        TransferId id,
        Guid sourceWalletId,
        Guid targetWalletId,
        string currencyCode,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset createdAtUtc)
    {
        if (sourceWalletId == Guid.Empty)
        {
            throw new ArgumentException("Source wallet id cannot be empty.", nameof(sourceWalletId));
        }

        if (targetWalletId == Guid.Empty)
        {
            throw new ArgumentException("Target wallet id cannot be empty.", nameof(targetWalletId));
        }

        if (sourceWalletId == targetWalletId)
        {
            throw new InvalidOperationException("Source and target wallets must be different.");
        }

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Transfer amount must be positive.");
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Transfer timestamp must be UTC.", nameof(createdAtUtc));
        }

        return new TransferIntent(
            id,
            sourceWalletId,
            targetWalletId,
            NormalizeCurrency(currencyCode),
            amountMinor,
            correlationId,
            createdAtUtc);
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ISO-like letters.", nameof(currencyCode));
        }

        return normalized;
    }
}
