namespace UniversalWallet.Api.Domain.Balance;

public sealed class WalletBalanceProjection
{
	public WalletBalanceProjection(
		Guid walletId,
		string currency,
		decimal ledgerBalance,
		decimal availableBalance,
		decimal pendingBalance,
		decimal reservedBalance,
		decimal incomingToday,
		decimal outgoingToday,
		long lastLedgerPosition,
		DateTimeOffset updatedAt)
	{
		WalletId = walletId;
		Currency = currency;
		LedgerBalance = ledgerBalance;
		AvailableBalance = availableBalance;
		PendingBalance = pendingBalance;
		ReservedBalance = reservedBalance;
		IncomingToday = incomingToday;
		OutgoingToday = outgoingToday;
		LastLedgerPosition = lastLedgerPosition;
		UpdatedAt = updatedAt;
	}

	public Guid WalletId { get; }
	public string Currency { get; }
	public decimal LedgerBalance { get; }
	public decimal AvailableBalance { get; }
	public decimal PendingBalance { get; }
	public decimal ReservedBalance { get; }
	public decimal IncomingToday { get; }
	public decimal OutgoingToday { get; }
	public long LastLedgerPosition { get; }
	public DateTimeOffset UpdatedAt { get; }
}

public sealed class WalletBalanceProjectionState
{
	public WalletBalanceProjectionState(
		WalletBalanceProjection projection,
		long currentLedgerPosition,
		bool isUpToDate,
		bool wasLagging)
	{
		Projection = projection;
		CurrentLedgerPosition = currentLedgerPosition;
		IsUpToDate = isUpToDate;
		WasLagging = wasLagging;
	}

	public WalletBalanceProjection Projection { get; }
	public long CurrentLedgerPosition { get; }
	public bool IsUpToDate { get; }
	public bool WasLagging { get; }
}
