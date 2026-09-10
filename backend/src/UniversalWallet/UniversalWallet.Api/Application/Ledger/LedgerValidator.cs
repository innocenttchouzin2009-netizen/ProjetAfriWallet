using UniversalWallet.Api.Domain.Ledger;

namespace UniversalWallet.Api.Application.Ledger;

public sealed class LedgerValidator
{
	public (bool Ok, string Code, string Message) ValidatePosting(PostTransactionRequest request, IReadOnlyCollection<WalletSnapshot> wallets)
	{
		if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
		{
			return (false, "IDEMPOTENCY_KEY_REQUIRED", "Idempotency key is required.");
		}

		if (string.IsNullOrWhiteSpace(request.Awid))
		{
			return (false, "AWID_REQUIRED", "AWID is required.");
		}

		if (string.IsNullOrWhiteSpace(request.Currency))
		{
			return (false, "CURRENCY_REQUIRED", "Currency is required.");
		}

		if (request.Lines.Count < 2)
		{
			return (false, "LINES_REQUIRED", "At least two ledger lines are required.");
		}

		if (wallets.Count != request.Lines.Count)
		{
			return (false, "WALLET_NOT_FOUND", "Every ledger line must target an existing wallet.");
		}

		var debitTotal = request.Lines.Where(line => line.EntryType == EntryType.Debit).Sum(line => line.Amount);
		var creditTotal = request.Lines.Where(line => line.EntryType == EntryType.Credit).Sum(line => line.Amount);
		if (debitTotal != creditTotal)
		{
			return (false, "LEDGER_MISMATCH", "Debits and credits must balance exactly.");
		}

		foreach (var line in request.Lines)
		{
			if (line.Amount <= 0)
			{
				return (false, "AMOUNT_INVALID", "Ledger line amounts must be positive.");
			}

			var wallet = wallets.FirstOrDefault(candidate => candidate.WalletId == line.WalletId);
			if (wallet is null)
			{
				return (false, "WALLET_NOT_FOUND", "A ledger line wallet was not found.");
			}

			if (!string.Equals(wallet.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
			{
				return (false, "CURRENCY_MISMATCH", "Wallet currency must match transaction currency.");
			}

			if (wallet.IsClosed)
			{
				return (false, "WALLET_CLOSED", "Closed wallets cannot receive ledger postings.");
			}
		}

		return (true, string.Empty, string.Empty);
	}

	public (bool Ok, string Code, string Message) ValidateReversal(LedgerTransaction originalTransaction)
	{
		if (originalTransaction.Status == LedgerTransactionStatus.Reversed)
		{
			return (false, "TRANSACTION_ALREADY_REVERSED", "The transaction has already been reversed.");
		}

		return (true, string.Empty, string.Empty);
	}
}

public sealed class WalletSnapshot
{
	public WalletSnapshot(Guid walletId, string currency, bool isClosed)
	{
		WalletId = walletId;
		Currency = currency;
		IsClosed = isClosed;
	}

	public Guid WalletId { get; }
	public string Currency { get; }
	public bool IsClosed { get; }
}
