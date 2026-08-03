namespace UniversalWallet.Api.Domain.Balance;

public sealed class BalanceSnapshot
{
	public BalanceSnapshot(
		Guid walletId,
		string currency,
		decimal ledgerBalance,
		decimal availableBalance,
		decimal pendingBalance,
		decimal reservedBalance,
		decimal incomingToday,
		decimal outgoingToday,
		long ledgerPosition,
		DateTimeOffset createdAt)
	{
		WalletId = walletId;
		Currency = currency;
		LedgerBalance = ledgerBalance;
		AvailableBalance = availableBalance;
		PendingBalance = pendingBalance;
		ReservedBalance = reservedBalance;
		IncomingToday = incomingToday;
		OutgoingToday = outgoingToday;
		LedgerPosition = ledgerPosition;
		CreatedAt = createdAt;
	}

	public Guid WalletId { get; }
	public string Currency { get; }
	public decimal LedgerBalance { get; }
	public decimal AvailableBalance { get; }
	public decimal PendingBalance { get; }
	public decimal ReservedBalance { get; }
	public decimal IncomingToday { get; }
	public decimal OutgoingToday { get; }
	public long LedgerPosition { get; }
	public DateTimeOffset CreatedAt { get; }
}
