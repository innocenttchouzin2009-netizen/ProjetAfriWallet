namespace UniversalWallet.Api.Domain.Balance;

public sealed class ProjectionVersion
{
	public ProjectionVersion(Guid walletId, long ledgerPosition, long version, DateTimeOffset updatedAt)
	{
		WalletId = walletId;
		LedgerPosition = ledgerPosition;
		Version = version;
		UpdatedAt = updatedAt;
	}

	public Guid WalletId { get; }
	public long LedgerPosition { get; }
	public long Version { get; }
	public DateTimeOffset UpdatedAt { get; }
}
