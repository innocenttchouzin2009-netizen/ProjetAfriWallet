using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

var walletRepository = new InMemoryWalletRepository();
var ledgerRepository = new InMemoryLedgerRepository();
var journalRepository = new InMemoryLedgerJournalRepository();
var validator = new LedgerValidator();
var postingService = new LedgerPostingService(walletRepository, ledgerRepository, journalRepository, validator);
var postHandler = new PostTransactionHandler(postingService);

var projectionRepository = new InMemoryBalanceProjectionRepository();
var snapshotRepository = new InMemoryBalanceSnapshotRepository();
var versionRepository = new InMemoryProjectionVersionRepository();
var currencyReader = new WalletCurrencyReader(walletRepository);
var balanceService = new BalanceProjectionService(ledgerRepository, currencyReader, projectionRepository, snapshotRepository, versionRepository);

var failures = new List<string>();

Run("initial rebuild on empty ledger", () =>
{
	var wallet = walletRepository.Create("AWID_BAL_TEST_001", WalletType.Personal, "EUR");
	var projection = balanceService.RebuildFromLedger(wallet.Id);
	Assert(projection.LastLedgerPosition == 0, "position should be zero on empty ledger");
	Assert(projection.LedgerBalance == 0m, "ledger balance should be zero on empty ledger");
	Assert(projection.AvailableBalance == 0m, "available should be zero on empty ledger");
});

Run("projection tracks available compartment", () =>
{
	var source = walletRepository.Create("AWID_BAL_TEST_002", WalletType.Personal, "EUR");
	var target = walletRepository.Create("AWID_BAL_TEST_002", WalletType.Business, "EUR");

	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "bal-proj-1",
		Awid = "AWID_BAL_TEST_002",
		Currency = "EUR",
		Reference = "BAL-PAY-001",
		CorrelationId = "bal-corr-1",
		PostedBy = "tester",
		Session = "bal-session-1",
		Device = "bal-device-1",
		Lines =
		[
			new LedgerLineRequest { WalletId = source.Id, EntryType = EntryType.Debit, Amount = 100m, Description = "transfer out", Compartment = LedgerBalanceCompartment.Available },
			new LedgerLineRequest { WalletId = target.Id, EntryType = EntryType.Credit, Amount = 100m, Description = "transfer in", Compartment = LedgerBalanceCompartment.Available }
		]
	});

	var srcProjection = balanceService.GetProjectionState(source.Id).Projection;
	var dstProjection = balanceService.GetProjectionState(target.Id).Projection;
	Assert(srcProjection.AvailableBalance == -100m, "source available balance should be -100");
	Assert(dstProjection.AvailableBalance == 100m, "target available balance should be +100");
	Assert(srcProjection.LedgerBalance == -100m, "source ledger balance should match signed amount");
	Assert(dstProjection.LedgerBalance == 100m, "target ledger balance should match signed amount");
});

Run("projection tracks pending and reserved compartments", () =>
{
	var wallet = walletRepository.Create("AWID_BAL_TEST_003", WalletType.Personal, "EUR");
	var counter = walletRepository.Create("AWID_BAL_TEST_003", WalletType.Business, "EUR");

	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "bal-proj-2",
		Awid = "AWID_BAL_TEST_003",
		Currency = "EUR",
		Reference = "BAL-PENDING-001",
		CorrelationId = "bal-corr-2",
		PostedBy = "tester",
		Session = "bal-session-2",
		Device = "bal-device-2",
		Lines =
		[
			new LedgerLineRequest { WalletId = wallet.Id, EntryType = EntryType.Credit, Amount = 50m, Description = "pending in", Compartment = LedgerBalanceCompartment.Pending },
			new LedgerLineRequest { WalletId = counter.Id, EntryType = EntryType.Debit, Amount = 50m, Description = "pending out", Compartment = LedgerBalanceCompartment.Pending }
		]
	});

	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "bal-proj-3",
		Awid = "AWID_BAL_TEST_003",
		Currency = "EUR",
		Reference = "BAL-RESERVE-001",
		CorrelationId = "bal-corr-3",
		PostedBy = "tester",
		Session = "bal-session-3",
		Device = "bal-device-3",
		Lines =
		[
			new LedgerLineRequest { WalletId = wallet.Id, EntryType = EntryType.Credit, Amount = 20m, Description = "reserved in", Compartment = LedgerBalanceCompartment.Reserved },
			new LedgerLineRequest { WalletId = counter.Id, EntryType = EntryType.Debit, Amount = 20m, Description = "reserved out", Compartment = LedgerBalanceCompartment.Reserved }
		]
	});

	var projection = balanceService.GetProjectionState(wallet.Id).Projection;
	Assert(projection.PendingBalance == 50m, "pending should reflect pending compartment lines");
	Assert(projection.ReservedBalance == 20m, "reserved should reflect reserved compartment lines");
	Assert(projection.AvailableBalance == 0m, "available should remain unchanged");
	Assert(projection.LedgerBalance == 70m, "ledger balance is sum of signed amounts across compartments");
});

Run("lagging projection detection and incremental catch-up", () =>
{
	var source = walletRepository.Create("AWID_BAL_TEST_004", WalletType.Personal, "EUR");
	var target = walletRepository.Create("AWID_BAL_TEST_004", WalletType.Business, "EUR");

	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "bal-proj-4",
		Awid = "AWID_BAL_TEST_004",
		Currency = "EUR",
		Reference = "BAL-LAG-001",
		CorrelationId = "bal-corr-4",
		PostedBy = "tester",
		Session = "bal-session-4",
		Device = "bal-device-4",
		Lines =
		[
			new LedgerLineRequest { WalletId = source.Id, EntryType = EntryType.Debit, Amount = 10m, Description = "lag source", Compartment = LedgerBalanceCompartment.Available },
			new LedgerLineRequest { WalletId = target.Id, EntryType = EntryType.Credit, Amount = 10m, Description = "lag target", Compartment = LedgerBalanceCompartment.Available }
		]
	});

	var firstState = balanceService.GetProjectionState(target.Id);
	Assert(firstState.IsUpToDate, "first state should be up to date after initial projection");

	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "bal-proj-5",
		Awid = "AWID_BAL_TEST_004",
		Currency = "EUR",
		Reference = "BAL-LAG-002",
		CorrelationId = "bal-corr-5",
		PostedBy = "tester",
		Session = "bal-session-5",
		Device = "bal-device-5",
		Lines =
		[
			new LedgerLineRequest { WalletId = source.Id, EntryType = EntryType.Debit, Amount = 5m, Description = "lag source 2", Compartment = LedgerBalanceCompartment.Available },
			new LedgerLineRequest { WalletId = target.Id, EntryType = EntryType.Credit, Amount = 5m, Description = "lag target 2", Compartment = LedgerBalanceCompartment.Available }
		]
	});

	var secondState = balanceService.GetProjectionState(target.Id);
	Assert(secondState.WasLagging, "state should report lag before incremental catch-up");
	Assert(secondState.IsUpToDate, "state should be up to date after incremental catch-up");
	Assert(secondState.Projection.AvailableBalance == 15m, "incremental projection should apply missing ledger records");
});

Run("rebuild determinism", () =>
{
	var wallet = walletRepository.Create("AWID_BAL_TEST_005", WalletType.Personal, "EUR");
	var counter = walletRepository.Create("AWID_BAL_TEST_005", WalletType.Business, "EUR");

	postHandler.Handle(new PostTransactionRequest
	{
		IdempotencyKey = "bal-proj-6",
		Awid = "AWID_BAL_TEST_005",
		Currency = "EUR",
		Reference = "BAL-REBUILD-001",
		CorrelationId = "bal-corr-6",
		PostedBy = "tester",
		Session = "bal-session-6",
		Device = "bal-device-6",
		Lines =
		[
			new LedgerLineRequest { WalletId = wallet.Id, EntryType = EntryType.Credit, Amount = 33m, Description = "rebuild credit", Compartment = LedgerBalanceCompartment.Available },
			new LedgerLineRequest { WalletId = counter.Id, EntryType = EntryType.Debit, Amount = 33m, Description = "rebuild debit", Compartment = LedgerBalanceCompartment.Available }
		]
	});

	var before = balanceService.GetProjectionState(wallet.Id).Projection;
	var rebuilt = balanceService.RebuildFromLedger(wallet.Id);
	Assert(before.LedgerBalance == rebuilt.LedgerBalance, "rebuild should preserve computed balance");
	Assert(before.LastLedgerPosition == rebuilt.LastLedgerPosition, "rebuild should end at same ledger position");
	var version = balanceService.GetVersion(wallet.Id);
	Assert(version is not null && version.LedgerPosition == rebuilt.LastLedgerPosition, "projection version should match rebuilt ledger position");
});

if (failures.Count == 0)
{
	Console.WriteLine("All AFW-DLV-0004.3 balance projection scenarios passed.");
	return;
}

Console.WriteLine("AFW-DLV-0004.3 balance projection scenarios failed:");
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

void Assert(bool condition, string message)
{
	if (!condition)
	{
		throw new Exception(message);
	}
}
