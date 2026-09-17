using AfriWallet.Wallet.Domain;

namespace AfriWallet.Timeline.Domain;

public enum FinancialTimelineEntryKind
{
    MoneyTransfer = 1,
    PaymentRequest = 2,
    LedgerPosting = 3,
    CardTransaction = 4,
    BankTransfer = 5,
    MobileMoneyTransaction = 6,
    Refund = 7,
    Reversal = 8,
    Fee = 9,
    Adjustment = 10
}

public enum FinancialTimelineDirection
{
    Incoming = 1,
    Outgoing = 2,
    Neutral = 3
}

public enum FinancialTimelineState
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Declined = 5,
    Expired = 6
}

public sealed record TimelineSourceReference
{
    private TimelineSourceReference(string sourceSystem, string sourceId)
    {
        SourceSystem = sourceSystem;
        SourceId = sourceId;
    }

    public string SourceSystem { get; }
    public string SourceId { get; }
    public string Key => $"{SourceSystem}:{SourceId}";

    public static TimelineSourceReference Create(string sourceSystem, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceSystem))
            throw new ArgumentException("Source system is required.", nameof(sourceSystem));
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source id is required.", nameof(sourceId));

        var normalizedSystem = sourceSystem.Trim().ToLowerInvariant();
        if (normalizedSystem.Length > 64)
            throw new ArgumentException("Source system cannot exceed 64 characters.", nameof(sourceSystem));
        if (normalizedSystem.Any(char.IsWhiteSpace))
            throw new ArgumentException("Source system cannot contain whitespace.", nameof(sourceSystem));

        if (!string.Equals(sourceId, sourceId.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("Source id cannot contain surrounding whitespace.", nameof(sourceId));
        if (sourceId.Length > 128)
            throw new ArgumentException("Source id cannot exceed 128 characters.", nameof(sourceId));

        return new TimelineSourceReference(normalizedSystem, sourceId);
    }
}

public sealed record FinancialTimelineEntry
{
    private FinancialTimelineEntry(
        Guid ownerId,
        WalletId walletId,
        TimelineSourceReference source,
        FinancialTimelineEntryKind kind,
        FinancialTimelineDirection direction,
        FinancialTimelineState state,
        Currency currency,
        long amountMinor,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset updatedAtUtc,
        string? summary)
    {
        OwnerId = ownerId;
        WalletId = walletId;
        Source = source;
        Kind = kind;
        Direction = direction;
        State = state;
        Currency = currency;
        AmountMinor = amountMinor;
        OccurredAtUtc = occurredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Summary = summary;
    }

    public Guid OwnerId { get; }
    public WalletId WalletId { get; }
    public TimelineSourceReference Source { get; }
    public FinancialTimelineEntryKind Kind { get; }
    public FinancialTimelineDirection Direction { get; }
    public FinancialTimelineState State { get; }
    public Currency Currency { get; }
    public long AmountMinor { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public string? Summary { get; }

    public static FinancialTimelineEntry Create(
        Guid ownerId,
        WalletId walletId,
        TimelineSourceReference source,
        FinancialTimelineEntryKind kind,
        FinancialTimelineDirection direction,
        FinancialTimelineState state,
        Currency currency,
        long amountMinor,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset updatedAtUtc,
        string? summary = null)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        if (walletId.Value == Guid.Empty)
            throw new ArgumentException("Wallet id cannot be empty.", nameof(walletId));
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(currency);
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(direction))
            throw new ArgumentOutOfRangeException(nameof(direction));
        if (!Enum.IsDefined(state))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Timeline amount must be positive.");
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < occurredAtUtc)
            throw new ArgumentException("Updated timestamp cannot precede occurrence timestamp.", nameof(updatedAtUtc));

        var normalizedSummary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        if (normalizedSummary is not null && normalizedSummary.Length > 160)
            throw new ArgumentException("Timeline summary cannot exceed 160 characters.", nameof(summary));

        return new FinancialTimelineEntry(
            ownerId,
            walletId,
            source,
            kind,
            direction,
            state,
            currency,
            amountMinor,
            occurredAtUtc,
            updatedAtUtc,
            normalizedSummary);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
