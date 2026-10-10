using LedgerEntry = UniversalWallet.Api.Domain.Ledger.LedgerEntry;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Application.Ledger;

public sealed class LedgerPostingService
{
	private readonly IWalletRepository _walletRepository;
	private readonly ILedgerRepository _ledgerRepository;
	private readonly ILedgerJournalRepository _journalRepository;
	private readonly LedgerValidator _validator;

	public LedgerPostingService(
		IWalletRepository walletRepository,
		ILedgerRepository ledgerRepository,
		ILedgerJournalRepository journalRepository,
		LedgerValidator validator)
	{
		_walletRepository = walletRepository;
		_ledgerRepository = ledgerRepository;
		_journalRepository = journalRepository;
		_validator = validator;
	}

	public PostingResult Post(PostTransactionRequest request)
	{
		lock (_sync)
		{
		var transactionId = request.TransactionId ?? Guid.CreateVersion7();
		if (_ledgerRepository.TransactionExists(transactionId))
		{
			return PostingResult.Rejected("TRANSACTION_DUPLICATED", "Transaction id already exists.", LedgerEventType.LedgerMismatchDetected);
		}

		if (_ledgerRepository.TryGetByIdempotencyKey(request.IdempotencyKey, out var existing))
		{
			return _ledgerRepository.GetPostingResult(existing.TransactionId) ?? PostingResult.Rejected("IDEMPOTENT_REPLAY_FAILED", "Duplicate idempotency key could not be resolved.", LedgerEventType.LedgerMismatchDetected);
		}

		var wallets = request.Lines
			.Select(line => _walletRepository.GetById(line.WalletId))
			.Where(wallet => wallet is not null)
			.Select(wallet => new WalletSnapshot(wallet!.Id, wallet.Currency, wallet.Status == WalletStatus.Closed))
			.ToList();

		var validation = _validator.ValidatePosting(request, wallets);
		if (!validation.Ok)
		{
			var rejected = PostingResult.Rejected(validation.Code, validation.Message, LedgerEventType.LedgerMismatchDetected);
			_ledgerRepository.StoreRejectedTransaction(new LedgerTransaction(transactionId, LedgerTransactionStatus.Rejected, request.Currency, DateTimeOffset.UtcNow, request.Reference, request.CorrelationId, request.PostedBy, request.Awid, request.Session, request.Device, request.IdempotencyKey), request.IdempotencyKey, rejected);
			return rejected;
		}

		var postedAt = DateTimeOffset.UtcNow;
		var transaction = new LedgerTransaction(transactionId, LedgerTransactionStatus.Posted, request.Currency, postedAt, request.Reference, request.CorrelationId, request.PostedBy, request.Awid, request.Session, request.Device, request.IdempotencyKey);
		var entries = BuildEntries(request, transaction, postedAt);
		var result = PostingResult.Success(transaction, entries, LedgerEventType.LedgerBalanced);
		_ledgerRepository.StorePostedTransaction(transaction, entries, request.IdempotencyKey, result);
		return result;
		}
	}

	public PostingResult Reverse(ReverseTransactionRequest request)
	{
		lock (_sync)
		{
		if (_ledgerRepository.TryGetByIdempotencyKey(request.IdempotencyKey, out var existing))
		{
			return _ledgerRepository.GetPostingResult(existing.TransactionId) ?? PostingResult.Rejected("IDEMPOTENT_REPLAY_FAILED", "Duplicate idempotency key could not be resolved.", LedgerEventType.LedgerMismatchDetected);
		}

		var originalTransaction = _ledgerRepository.GetTransaction(request.TransactionId);
		if (originalTransaction is null)
		{
			return PostingResult.Rejected("TRANSACTION_NOT_FOUND", "Transaction not found.", LedgerEventType.LedgerMismatchDetected);
		}

		var validation = _validator.ValidateReversal(originalTransaction);
		if (!validation.Ok)
		{
			return PostingResult.Rejected(validation.Code, validation.Message, LedgerEventType.LedgerMismatchDetected);
		}

		var originalEntries = _ledgerRepository.GetEntriesByTransaction(originalTransaction.TransactionId);
		var reversalTransaction = new LedgerTransaction(Guid.CreateVersion7(), LedgerTransactionStatus.Reversed, originalTransaction.Currency, DateTimeOffset.UtcNow, request.Reference, request.CorrelationId, request.PostedBy, request.Awid, request.Session, request.Device, request.IdempotencyKey, originalTransaction.TransactionId);
		var reversalEntries = originalEntries.Select(entry => new LedgerEntry(
			Guid.CreateVersion7(),
			_ledgerRepository.ReserveNextPosition(),
			entry.JournalId,
			entry.WalletId,
			reversalTransaction.TransactionId,
			entry.EntryType == EntryType.Debit ? EntryType.Credit : EntryType.Debit,
			entry.Credit,
			entry.Debit,
			entry.Compartment,
			entry.Currency,
			request.Reference,
			$"Reversal of {entry.Description}",
			DateTimeOffset.UtcNow,
			request.PostedBy,
			request.CorrelationId,
			request.Awid,
			request.Session,
			request.Device,
			request.IdempotencyKey)).ToList();
		var result = PostingResult.Success(reversalTransaction, reversalEntries, LedgerEventType.TransactionReversed);
		_ledgerRepository.StorePostedTransaction(reversalTransaction, reversalEntries, request.IdempotencyKey, result);
		return result;
		}
	}

	private readonly object _sync = new();

	private IReadOnlyList<LedgerEntry> BuildEntries(PostTransactionRequest request, LedgerTransaction transaction, DateTimeOffset postedAt)
	{
		return request.Lines.Select(line => new LedgerEntry(
			Guid.CreateVersion7(),
			_ledgerRepository.ReserveNextPosition(),
			_journalRepository.GetOrCreateJournal(line.WalletId, request.Awid, request.Currency).JournalId,
			line.WalletId,
			transaction.TransactionId,
			line.EntryType,
			line.EntryType == EntryType.Debit ? line.Amount : 0m,
			line.EntryType == EntryType.Credit ? line.Amount : 0m,
			line.Compartment,
			request.Currency,
			request.Reference,
			line.Description,
			postedAt,
			request.PostedBy,
			request.CorrelationId,
			request.Awid,
			request.Session,
			request.Device,
			request.IdempotencyKey)).ToList();
	}
}
