using AfriWallet.Timeline.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Timeline.Application;

public sealed record FinancialActivityQuery
{
    private FinancialActivityQuery(Guid ownerId, WalletId? walletId)
    {
        OwnerId = ownerId;
        WalletId = walletId;
    }

    public Guid OwnerId { get; }
    public WalletId? WalletId { get; }

    public static FinancialActivityQuery Create(Guid ownerId, WalletId? walletId = null)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        if (walletId is not null && walletId.Value.Value == Guid.Empty)
            throw new ArgumentException("Wallet id cannot be empty.", nameof(walletId));

        return new FinancialActivityQuery(ownerId, walletId);
    }
}

public sealed record FinancialActivityResult(IReadOnlyList<FinancialActivity> Items)
{
    public static FinancialActivityResult Create(IReadOnlyList<FinancialActivity> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Any(item => item is null))
            throw new ArgumentException("Activity result cannot contain null entries.", nameof(items));

        return new FinancialActivityResult(items);
    }
}

public interface IFinancialActivityReader
{
    Task<FinancialActivityResult> ReadAsync(
        FinancialActivityQuery query,
        CancellationToken cancellationToken = default);
}
