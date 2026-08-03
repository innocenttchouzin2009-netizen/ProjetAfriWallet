using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Domain.Currency;
using UniversalWallet.Api.Infrastructure.Currency;

var repository = new InMemoryCurrencyRegistryRepository(seedDefaults: false);
var registry = new CurrencyRegistryService(repository);

var seedCurrencies = new[]
{
	new Currency("EUR", 978, "Euro", 2, "€", "Europe", CurrencyStatus.Active, DateTimeOffset.UtcNow),
	new Currency("USD", 840, "US Dollar", 2, "$", "North America", CurrencyStatus.Active, DateTimeOffset.UtcNow),
	new Currency("XPF", 953, "French Polynesia CFP Franc", 0, "₣", "Oceania", CurrencyStatus.Active, DateTimeOffset.UtcNow)
};

foreach (var currency in seedCurrencies)
{
	repository.Add(currency);
}

var failures = new List<string>();

Run("registers valid currency", () =>
{
	var currency = repository.Add(new Currency("CAD", 124, "Canadian Dollar", 2, "CA$", "North America", CurrencyStatus.Active, DateTimeOffset.UtcNow));
	Assert(currency.Code == "CAD", "registered currency should preserve code");
	Assert(currency.Status == CurrencyStatus.Active, "new currency should be active");
});

Run("normalizes currency code", () =>
{
	var currency = repository.Add(new Currency("xaf", 950, "Central African CFA Franc", 0, "FCFA", "Africa", CurrencyStatus.Active, DateTimeOffset.UtcNow));
	Assert(currency.Code == "XAF", "currency code should be normalized to uppercase");
});

Run("rejects invalid code", () =>
{
	AssertThrows<ArgumentException>(() => new Currency("12A", 840, "Invalid", 2, "$", "North America", CurrencyStatus.Active, DateTimeOffset.UtcNow));
});

Run("rejects duplicate currency", () =>
{
	AssertThrows<InvalidOperationException>(() => repository.Add(new Currency("EUR", 978, "Euro", 2, "€", "Europe", CurrencyStatus.Active, DateTimeOffset.UtcNow)));
});

Run("retrieves currency by code", () =>
{
	var currency = registry.GetRequired("EUR");
	Assert(currency.Code == "EUR", "currency should be retrievable by code");
});

Run("lists active currencies", () =>
{
	var active = registry.ListActive();
	Assert(active.Any(currency => currency.Code == "EUR"), "EUR should be active");
	Assert(active.Any(currency => currency.Code == "USD"), "USD should be active");
});

Run("disabled currency cannot create wallet", () =>
{
	repository.SetStatus("USD", CurrencyStatus.Disabled);
	AssertThrows<InvalidOperationException>(() => registry.RequireActive("USD"));
	repository.SetStatus("USD", CurrencyStatus.Active);
});

Run("reactivates currency", () =>
{
	repository.SetStatus("USD", CurrencyStatus.Disabled);
	repository.SetStatus("USD", CurrencyStatus.Active);
	var currency = registry.GetRequired("USD");
	Assert(currency.Status == CurrencyStatus.Active, "disabled currency should be reactivated");
});

Run("retired currency cannot be reactivated", () =>
{
	repository.SetStatus("USD", CurrencyStatus.Retired);
	AssertThrows<InvalidOperationException>(() => repository.SetStatus("USD", CurrencyStatus.Active));
});

Run("formats EUR amounts", () =>
{
	var amount = CurrencyAmount.FromMajor("EUR", 10.505m, 2, MidpointRounding.ToEven);
	Assert(amount.MinorValue == 1050, "EUR should round to 1050 minor units with ToEven");
});

Run("formats XAF amounts", () =>
{
	var amount = CurrencyAmount.FromMajor("XAF", 10.5m, 0, MidpointRounding.ToEven);
	Assert(amount.MinorValue == 10, "XAF should round to 10 minor units with ToEven");
});

Run("does not use double for financial calculations", () =>
{
	var amount = CurrencyAmount.FromMajor("EUR", 1.23m, 2, MidpointRounding.ToEven);
	Assert(amount.MinorValue == 123, "amount conversion should stay in decimal-based calculations");
});

if (failures.Count == 0)
{
	Console.WriteLine("All AFW-DLV-0004.4.1 currency scenarios passed.");
	return;
}

Console.WriteLine("AFW-DLV-0004.4.1 currency scenarios failed:");
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

void AssertThrows<TException>(Action action) where TException : Exception
{
	try
	{
		action();
		throw new Exception($"expected {typeof(TException).Name}");
	}
	catch (TException)
	{
	}
}
