using UniversalWallet.Api.WalletEngine;

var repository = new InMemoryWalletRepository();
var failures = new List<string>();

Run("create wallet", () =>
{
	var wallet = repository.Create("AWID_TEST_USER_001", WalletType.Personal, "XAF");
	Assert(wallet.Currency == "XAF", "currency must be XAF");
	Assert(wallet.Status == WalletStatus.Created, "status must be Created");
	Assert(wallet.WalletNumber.StartsWith("AFW-XAF-", StringComparison.Ordinal), "wallet number format mismatch");
});

Run("list wallets by awid", () =>
{
	repository.Create("AWID_TEST_USER_001", WalletType.Business, "EUR");
	var wallets = repository.ListByAwid("AWID_TEST_USER_001");
	Assert(wallets.Count == 2, "expected 2 wallets");
});

Run("get wallet by id", () =>
{
	var wallet = repository.Create("AWID_TEST_USER_002", WalletType.Savings, "USD");
	var found = repository.GetById(wallet.Id);
	Assert(found is not null, "wallet should exist");
	Assert(found!.Id == wallet.Id, "wallet id mismatch");
});

Run("duplicate wallet blocked", () =>
{
	repository.Create("AWID_DUP_001", WalletType.Personal, "GBP");
	ExpectInvalidOperation("WALLET_ALREADY_EXISTS", () =>
	{
		repository.Create("AWID_DUP_001", WalletType.Personal, "GBP");
	});
});

Run("unsupported currency blocked", () =>
{
	ExpectInvalidOperation("CURRENCY_NOT_SUPPORTED", () =>
	{
		repository.Create("AWID_CUR_001", WalletType.Personal, "XYZ");
	});
});

Run("status transition", () =>
{
	var wallet = repository.Create("AWID_STATE_001", WalletType.Personal, "CAD");
	var updated = repository.UpdateStatus(wallet.Id, WalletStatus.Active);
	Assert(updated is not null, "wallet should update");
	Assert(updated!.Status == WalletStatus.Active, "wallet should be active");
});

Run("closed wallet is immutable", () =>
{
	var wallet = repository.Create("AWID_STATE_002", WalletType.Personal, "CHF");
	repository.UpdateStatus(wallet.Id, WalletStatus.Closed);
	ExpectInvalidOperation("WALLET_CLOSED", () =>
	{
		repository.UpdateStatus(wallet.Id, WalletStatus.Active);
	});
});

Run("ledger starts empty", () =>
{
	var wallet = repository.Create("AWID_LEDGER_001", WalletType.Personal, "NGN");
	var entries = repository.GetLedger(wallet.Id);
	Assert(entries.Count == 0, "new wallet ledger should be empty");
});

if (failures.Count == 0)
{
	Console.WriteLine("All AFW-DLV-0004.1 scenarios passed.");
	return;
}

Console.WriteLine("AFW-DLV-0004.1 scenarios failed:");
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

void ExpectInvalidOperation(string code, Action action)
{
	try
	{
		action();
		throw new Exception($"Expected InvalidOperationException({code})");
	}
	catch (InvalidOperationException ex) when (ex.Message == code)
	{
		// Expected.
	}
}

void Assert(bool condition, string message)
{
	if (!condition)
	{
		throw new Exception(message);
	}
}
