using AfriWallet.Wallet.Domain;

namespace AfriWallet.Timeline.Domain;

public enum FinancialActivityKind
{
    LedgerPosting = 1,
    InternalTransfer = 2,
    P2PTransfer = 3,
    PaymentRequest = 4
}

public enum FinancialActivityDirection
{
    Incoming = 1,
    Outgoing = 2,
    Neutral = 3
}

public enum FinancialActivityState
{
    Pending = 1,
    Completed = 2,
    Declined = 3,
    Cancelled = 4,
    Expired = 5,
    Failed = 6
}

public sealed record FinancialActivitySource
{
    private FinancialActivitySource(string system, string id)
    {
        System = system;
        Id = id;
    }

    public string System { get; }
    public string Id { get; }
    public string Key => $"{System}:{Id}";

    public static FinancialActivitySource Create(string system, string id)
    {
        if (string.IsNullOrWhiteSpace(system))
            throw new ArgumentException("Source system is required.", nameof(system));
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Source id is required.", nameof(id));

        var normalizedSystem = system.Trim().ToLowerInvariant();
        if (normalizedSystem.Length > 64 || normalizedSystem.Any(char.IsWhiteSpace))
            throw new ArgumentException("Source system must be at most 64 characters without whitespace.", nameof(system));

        if (!string.Equals(id, id.Trim(), StringComparison.Ordinal) || id.Length > 128)
            throw new ArgumentException("Source id must be at most 128 characters without surrounding whitespace.", nameof(id));

        return new FinancialActivitySource(normalizedSystem, id);
    }
}

public sealed record FinancialActivity
{
    private FinancialActivity(
        Guid ownerId,
        WalletId walletId,
        FinancialActivitySource source,
        FinancialActivityKind kind,
        FinancialActivityDirection direction,
        FinancialActivityState state,
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
    public FinancialActivitySource Source { get; }
    public FinancialActivityKind Kind { get; }
    public FinancialActivityDirection Direction { get; }
    public FinancialActivityState State { get; }
    public Currency Currency { get; }
    public long AmountMinor { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public string? Summary { get; }

    public static FinancialActivity Create(
        Guid ownerId,
        WalletId walletId,
        FinancialActivitySource source,
        FinancialActivityKind kind,
        FinancialActivityDirection direction,
        FinancialActivityState state,
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

        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Activity amount must be positive.");

        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < occurredAtUtc)
            throw new ArgumentException("Updated timestamp cannot precede occurrence timestamp.", nameof(updatedAtUtc));

        var normalizedSummary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        if (normalizedSummary is not null && normalizedSummary.Length > 160)
            throw new ArgumentException("Summary cannot exceed 160 characters.", nameof(summary));

        return new FinancialActivity(
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
