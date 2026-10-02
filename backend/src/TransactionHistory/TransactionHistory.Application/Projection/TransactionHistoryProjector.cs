using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionHistory.Application.Contracts;

namespace AfriWallet.TransactionHistory.Application.Projection;

public sealed class TransactionHistoryProjector
{
    public TransactionHistoryItem Project(TransactionHistoryProjectionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.TransactionId == Guid.Empty)
        {
            throw new ArgumentException("Transaction id cannot be empty.", nameof(source));
        }

        if (source.AmountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(source), "Transaction amount must be positive.");
        }

        if (source.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Transaction timestamp must be UTC.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(source.CurrencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(source.Reference))
        {
            throw new ArgumentException("Transaction reference is required.", nameof(source));
        }

        var direction = source.WalletLedgerSide switch
        {
            LedgerSide.Credit => TransactionHistoryDirection.Incoming,
            LedgerSide.Debit => TransactionHistoryDirection.Outgoing,
            _ => throw new InvalidOperationException("Unsupported ledger side.")
        };

        return new TransactionHistoryItem(
            source.TransactionId,
            source.WalletId,
            source.AmountMinor,
            source.CurrencyCode.Trim().ToUpperInvariant(),
            direction,
            source.Status,
            source.OccurredAtUtc,
            source.Reference.Trim(),
            source.CounterpartyLabel);
    }

    public IReadOnlyList<TransactionHistoryItem> Project(
        IEnumerable<TransactionHistoryProjectionSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        return sources
            .Select(Project)
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.TransactionId)
            .ToArray();
    }
}
