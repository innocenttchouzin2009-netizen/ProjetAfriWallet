using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

var walletRepository = new InMemoryWalletRepository();
var ledgerRepository = new InMemoryLedgerRepository();
var journalRepository = new InMemoryLedgerJournalRepository();
var validator = new LedgerValidator();
var postingService = new LedgerPostingService(walletRepository, ledgerRepository, journalRepository, validator);
var postHandler = new PostTransactionHandler(postingService);
var reverseHandler = new ReverseTransactionHandler(postingService);

var failures = new List<string>();

Run("journal empty", () =>
{
	var wallet = SeedWallet("AWID_LEDGER_001", WalletType.Personal, "EUR");
	Assert(ledgerRepository.GetEntriesByWallet(wallet.Id).Count == 0, "new wallet history should be empty");
	Assert(journalRepository.GetByWalletId(wallet.Id) is null, "journal should not exist before first posting");
});

Run("balanced transaction accepted", () =>
{
	var alice = SeedWallet("AWID_LEDGER_002", WalletType.Personal, "EUR");
	var bob = SeedWallet("AWID_LEDGER_002", WalletType.Business, "EUR");
	var result = postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-balanced-1",
		Awid = "AWID_LEDGER_002",
		Currency = "EUR",
		Reference = "PAYMENT-001",
		CorrelationId = "corr-001",
		PostedBy = "tester",
		Session = "session-001",
		Device = "device-001",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 100m, Description = "Alice payment" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 100m, Description = "Bob receipt" }
		]
	});

	Assert(result.Accepted, "balanced transaction should be accepted");
	Assert(result.Events.Contains(LedgerEventType.LedgerBalanced), "balanced event should be emitted");
	Assert(result.Transaction is not null, "transaction should be returned");
	Assert(result.Transaction!.Status == LedgerTransactionStatus.Posted, "transaction status should be posted");
});

Run("double entry created", () =>
{
	var transaction = ledgerRepository.GetTransactions().First(transaction => transaction.Reference == "PAYMENT-001");
	var entries = ledgerRepository.GetEntriesByTransaction(transaction.TransactionId);
	Assert(entries.Count == 2, "balanced transaction should create two entries");
	Assert(entries.Count(entry => entry.EntryType == EntryType.Debit) == 1, "one debit entry expected");
	Assert(entries.Count(entry => entry.EntryType == EntryType.Credit) == 1, "one credit entry expected");
	Assert(entries.Sum(entry => entry.Debit) == entries.Sum(entry => entry.Credit), "entries must balance");
});

Run("unbalanced transaction rejected", () =>
{
	var alice = SeedWallet("AWID_LEDGER_003", WalletType.Personal, "EUR");
	var bob = SeedWallet("AWID_LEDGER_003", WalletType.Business, "EUR");
	var result = postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-unbalanced-1",
		Awid = "AWID_LEDGER_003",
		Currency = "EUR",
		Reference = "PAYMENT-002",
		CorrelationId = "corr-002",
		PostedBy = "tester",
		Session = "session-002",
		Device = "device-002",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 100m, Description = "Alice payment" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 99m, Description = "Bob receipt" }
		]
	});

	Assert(!result.Accepted, "unbalanced transaction should be rejected");
	Assert(result.Code == "LEDGER_MISMATCH", "expected mismatch code");
	Assert(result.Events.Contains(LedgerEventType.LedgerMismatchDetected), "mismatch event should be emitted");
});

Run("currency mismatch rejected", () =>
{
	var wallet = SeedWallet("AWID_LEDGER_004", WalletType.Personal, "XAF");
	var counterWallet = SeedWallet("AWID_LEDGER_004", WalletType.Business, "EUR");
	var result = postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-currency-1",
		Awid = "AWID_LEDGER_004",
		Currency = "USD",
		Reference = "PAYMENT-003",
		CorrelationId = "corr-003",
		PostedBy = "tester",
		Session = "session-003",
		Device = "device-003",
		Lines =
		[
			new LedgerLineRequest { WalletId = wallet.Id, EntryType = EntryType.Debit, Amount = 10m, Description = "wrong currency" },
			new LedgerLineRequest { WalletId = counterWallet.Id, EntryType = EntryType.Credit, Amount = 10m, Description = "wrong currency" }
		]
	});

	Assert(!result.Accepted, "currency mismatch should be rejected");
	Assert(result.Code == "CURRENCY_MISMATCH", "expected currency mismatch code");
});

Run("idempotence", () =>
{
	var alice = SeedWallet("AWID_LEDGER_005", WalletType.Personal, "EUR");
	var bob = SeedWallet("AWID_LEDGER_005", WalletType.Business, "EUR");
	var request = new PostTransactionRequest
	{
		IdempotencyKey = "ledger-idempotent-1",
		Awid = "AWID_LEDGER_005",
		Currency = "EUR",
		Reference = "PAYMENT-004",
		CorrelationId = "corr-004",
		PostedBy = "tester",
		Session = "session-004",
		Device = "device-004",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 25m, Description = "idempotent debit" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 25m, Description = "idempotent credit" }
		]
	};

	var first = postHandler.Handle(request);
	var second = postHandler.Handle(request);
	Assert(first.Accepted && second.Accepted, "idempotent requests should be accepted");
	Assert(first.Transaction!.TransactionId == second.Transaction!.TransactionId, "same transaction id expected on replay");
	Assert(ledgerRepository.GetTransactions().Count(transaction => transaction.Reference == "PAYMENT-004") == 1, "only one transaction should be stored");
});

Run("duplicate transaction rejected", () =>
{
	var alice = SeedWallet("AWID_LEDGER_006", WalletType.Personal, "EUR");
	var bob = SeedWallet("AWID_LEDGER_006", WalletType.Business, "EUR");
	var transactionId = Guid.CreateVersion7();
	var first = postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-duplicate-1",
		TransactionId = transactionId,
		Awid = "AWID_LEDGER_006",
		Currency = "EUR",
		Reference = "PAYMENT-005",
		CorrelationId = "corr-005",
		PostedBy = "tester",
		Session = "session-005",
		Device = "device-005",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 15m, Description = "duplicate debit" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 15m, Description = "duplicate credit" }
		]
	});

	var second = postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-duplicate-2",
		TransactionId = transactionId,
		Awid = "AWID_LEDGER_006",
		Currency = "EUR",
		Reference = "PAYMENT-005B",
		CorrelationId = "corr-005b",
		PostedBy = "tester",
		Session = "session-005b",
		Device = "device-005b",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 15m, Description = "duplicate debit" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 15m, Description = "duplicate credit" }
		]
	});

	Assert(first.Accepted, "first posting should succeed");
	Assert(!second.Accepted, "duplicate transaction id should be rejected");
	Assert(second.Code == "TRANSACTION_DUPLICATED", "expected duplicate code");
});

Run("reversal", () =>
{
	var original = ledgerRepository.GetTransactions().First(transaction => transaction.Reference == "PAYMENT-001");
	var result = reverseHandler.Handle(new ReverseTransactionRequest
	{
		IdempotencyKey = "ledger-reversal-1",
		TransactionId = original.TransactionId,
		Awid = "AWID_LEDGER_002",
		Reference = "REVERSAL-001",
		CorrelationId = "corr-rev-001",
		PostedBy = "tester",
		Session = "session-rev-001",
		Device = "device-rev-001"
	});

	Assert(result.Accepted, "reversal should be accepted");
	Assert(result.Transaction!.Status == LedgerTransactionStatus.Reversed, "reversal transaction status should be reversed");
	Assert(result.Entries.Count == 2, "reversal should create two entries");
	Assert(result.Entries.All(entry => entry.Reference == "REVERSAL-001"), "reversal reference should be propagated");
});

Run("append-only verified", () =>
{
	var walletEntries = ledgerRepository.GetEntriesByWallet(walletRepository.ListByAwid("AWID_LEDGER_002").First(wallet => wallet.Currency == "EUR").Id);
	Assert(walletEntries.Count >= 2, "history should retain original entries after reversal");
	Assert(typeof(UniversalWallet.Api.Domain.Ledger.LedgerEntry).GetProperties().All(property => property.SetMethod is null || !property.SetMethod.IsPublic), "ledger entries must not expose public setters");
});

Run("history sorted", () =>
{
	var alice = SeedWallet("AWID_LEDGER_007", WalletType.Personal, "EUR");
	var bob = SeedWallet("AWID_LEDGER_007", WalletType.Business, "EUR");
	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-history-1",
		Awid = "AWID_LEDGER_007",
		Currency = "EUR",
		Reference = "PAYMENT-006",
		CorrelationId = "corr-006",
		PostedBy = "tester",
		Session = "session-006",
		Device = "device-006",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 7m, Description = "older" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 7m, Description = "older" }
		]
	});
	Thread.Sleep(10);
	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "ledger-history-2",
		Awid = "AWID_LEDGER_007",
		Currency = "EUR",
		Reference = "PAYMENT-007",
		CorrelationId = "corr-007",
		PostedBy = "tester",
		Session = "session-007",
		Device = "device-007",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 9m, Description = "newer" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 9m, Description = "newer" }
		]
	});

	var entries = ledgerRepository.GetEntriesByWallet(alice.Id);
	Assert(entries.Count >= 2, "wallet history should contain multiple entries");
	Assert(entries[0].Reference == "PAYMENT-007", "history should be sorted newest first");
});

Run("concurrency idempotence", () =>
{
	var alice = SeedWallet("AWID_LEDGER_008", WalletType.Personal, "EUR");
	var bob = SeedWallet("AWID_LEDGER_008", WalletType.Business, "EUR");
	var request = new PostTransactionRequest
	{
		IdempotencyKey = "ledger-concurrency-1",
		Awid = "AWID_LEDGER_008",
		Currency = "EUR",
		Reference = "PAYMENT-008",
		CorrelationId = "corr-008",
		PostedBy = "tester",
		Session = "session-008",
		Device = "device-008",
		Lines =
		[
			new LedgerLineRequest { WalletId = alice.Id, EntryType = EntryType.Debit, Amount = 30m, Description = "parallel debit" },
			new LedgerLineRequest { WalletId = bob.Id, EntryType = EntryType.Credit, Amount = 30m, Description = "parallel credit" }
		]
	};

	Parallel.For(0, 6, _ => postHandler.Handle(request));
	Assert(ledgerRepository.GetTransactions().Count(transaction => transaction.Reference == "PAYMENT-008") == 1, "concurrent identical requests should produce one transaction");
});

if (failures.Count == 0)
{
	Console.WriteLine("All AFW-DLV-0004.2 scenarios passed.");
	return;
}

Console.WriteLine("AFW-DLV-0004.2 scenarios failed:");
foreach (var failure in failures)
{
	Console.WriteLine($" - {failure}");
}

Environment.ExitCode = 1;

void Run(string name, Action scenario)
{
	try
	{
		scenario();
		Console.WriteLine($"[OK] {name}");
	}
	catch (Exception ex)
	{
		failures.Add($"{name}: {ex.Message}");
		Console.WriteLine($"[KO] {name} -> {ex.Message}");
	}
}

Wallet SeedWallet(string awid, WalletType walletType, string currency)
{
	return walletRepository.Create(awid, walletType, currency);
}

void Assert(bool condition, string message)
{
	if (!condition)
	{
		throw new Exception(message);
	}
}
