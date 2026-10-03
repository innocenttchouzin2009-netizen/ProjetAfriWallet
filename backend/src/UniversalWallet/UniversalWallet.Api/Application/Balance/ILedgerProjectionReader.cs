using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Application.Balance;

public sealed class LedgerProjectionRecord
{
	public LedgerProjectionRecord(
		Guid walletId,
		long position,
		decimal signedAmount,
		string currency,
		LedgerBalanceCompartment compartment,
		DateTimeOffset postedAt)
	{
		WalletId = walletId;
		Position = position;
		SignedAmount = signedAmount;
		Currency = currency;
		Compartment = compartment;
		PostedAt = postedAt;
	}

	public Guid WalletId { get; }
	public long Position { get; }
	public decimal SignedAmount { get; }
	public string Currency { get; }
	public LedgerBalanceCompartment Compartment { get; }
	public DateTimeOffset PostedAt { get; }
}

public interface ILedgerProjectionReader
{
	long GetLatestPosition(Guid walletId);
	IReadOnlyList<LedgerProjectionRecord> ReadWalletEntries(Guid walletId, long fromExclusivePosition);
}
