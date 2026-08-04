using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Application.FxQuotes;
using UniversalWallet.Api.Domain.Currency;
using UniversalWallet.Api.Domain.Fx;
using UniversalWallet.Api.Domain.FxQuotes;
using UniversalWallet.Api.Infrastructure.Currency;
using UniversalWallet.Api.Infrastructure.Fx;
using UniversalWallet.Api.Infrastructure.FxQuotes;

var currencyRegistry = new InMemoryCurrencyRegistryRepository();
var provider = new MutableRateProvider();
var cache = new FxRateCache(TimeSpan.FromMilliseconds(5));
var rateRepository = new InMemoryFxRateRepository();
var historyRepository = new InMemoryFxHistoryRepository();
var rateHistory = new InMemoryFxRateHistoryRepository();
var conversionRepository = new InMemoryFxConversionRepository();
var currencyService = new CurrencyRegistryService(currencyRegistry);
var service = new FxEngineService(currencyService, provider, cache, rateRepository, historyRepository, conversionRepository);
provider.SetRate("EUR", "XAF", 655.957m);
provider.SetRate("XAF", "EUR", 1m / 655.957m);
provider.SetRate("EUR", "USD", 1.09m);
provider.SetRate("USD", "EUR", 0.917431m);
provider.SetRate("USD", "XAF", 601.0m);
provider.SetRate("XAF", "USD", 1m / 601.0m);
var quoteRepository = new InMemoryQuoteRepository();
var quoteCalculator = new QuoteCalculator();
var quoteHandler = new CreateQuoteHandler(service, quoteCalculator, quoteRepository, currencyService);

var failures = new List<string>();

Run("currency registry exposes ISO currencies", () =>
{
	var currencies = service.ListCurrencies();
	Assert(currencies.Any(currency => currency.Code == "EUR"), "EUR should be registered");
	Assert(currencies.Any(currency => currency.Code == "XAF"), "XAF should be registered");
});

Run("disabled currency is rejected", () =>
{
	currencyRegistry.SetStatus("USD", CurrencyStatus.Disabled);
	AssertThrows("CURRENCY_DISABLED", () => service.GetLatestRate("EUR", "USD"));
	currencyRegistry.SetStatus("USD", CurrencyStatus.Active);
});

Run("EUR to XAF conversion uses central bank rate", () =>
{
	provider.SetRate("EUR", "XAF", 655.957m);
	var conversion = service.Convert(new ConvertCurrencyRequest { From = "EUR", To = "XAF", AmountMinor = 100 });
	Assert(conversion.SourceAmount.MinorValue == 100, "source amount should be preserved");
	Assert(conversion.TargetAmount.CurrencyCode == "XAF", "target currency should be XAF");
	Assert(conversion.TargetAmount.MinorValue == 656, "100 EUR should round to 656 XAF minor units");
	Assert(conversion.ExchangeRate.Rate == 655.957m, "rate should be the configured value");
});

Run("XAF to EUR conversion uses inverse rate", () =>
{
	provider.SetRate("XAF", "EUR", 0.001524m);
	var conversion = service.Convert(new ConvertCurrencyRequest { From = "XAF", To = "EUR", AmountMinor = 1000 });
	Assert(conversion.TargetAmount.CurrencyCode == "EUR", "target currency should be EUR");
	Assert(conversion.TargetAmount.MinorValue == 152, "1000 XAF should round to 152 EUR minor units");
});

Run("same currency conversion is identity", () =>
{
	var conversion = service.Convert(new ConvertCurrencyRequest { From = "EUR", To = "EUR", AmountMinor = 12345 });
	Assert(conversion.TargetAmount.MinorValue == 12345, "same-currency conversion should preserve amount");
	Assert(conversion.ExchangeRate.Rate == 1m, "same-currency rate should be 1");
});

Run("inverse rate is derived when direct quote is unavailable", () =>
{
	var inverseOnlyProvider = new MutableRateProvider();
	inverseOnlyProvider.SetRate("USD", "EUR", 0.91m);
	var inverseOnlyService = new FxEngineService(
		currencyService,
		inverseOnlyProvider,
		new FxRateCache(TimeSpan.FromMilliseconds(5)),
		new InMemoryFxRateRepository(),
		new InMemoryFxHistoryRepository(),
		new InMemoryFxConversionRepository());

	var rate = inverseOnlyService.GetLatestRate("EUR", "USD");
	Assert(rate.Rate == 1m / 0.91m, "EUR/USD should be derived from USD/EUR reciprocal rate");
});

Run("publishing a valid rate stores it and keeps history", () =>
{
	var refreshed = service.UpdateRate(new UpdateExchangeRateRequest { BaseCurrency = "EUR", QuoteCurrency = "XAF", Rate = 660m, Provider = "TestProvider" });
	Assert(refreshed.Rate == 660m, "published rate should be stored");
	Assert(service.RateHistory("EUR", "XAF").Any(), "published rate should be queryable from rate history");
	Assert(service.GetHistory("EUR", "XAF").Any(), "published rate should be recorded in audit history");
});

Run("replacing an active rate keeps the prior version", () =>
{
	service.UpdateRate(new UpdateExchangeRateRequest { BaseCurrency = "EUR", QuoteCurrency = "XAF", Rate = 661m, Provider = "TestProvider" });
	var history = service.RateHistory("EUR", "XAF");
	Assert(history.Count >= 2, "rate history should retain prior versions");
});

Run("unknown currency is rejected", () =>
{
	AssertThrows("CURRENCY_NOT_FOUND", () => service.GetLatestRate("ZZZ", "XAF"));
});

Run("rate cache expires", () =>
{
	provider.ResetCallCount();
	provider.SetRate("USD", "EUR", 0.91m);
	service.GetLatestRate("USD", "EUR");
	service.GetLatestRate("USD", "EUR");
	Assert(provider.CallCount == 1, "second call should be served from cache");
	Thread.Sleep(15);
	service.GetLatestRate("USD", "EUR");
	Assert(provider.CallCount >= 2, "cache expiry should trigger provider refresh");
});

Run("provider unavailable is surfaced", () =>
{
	provider.IsAvailable = false;
	AssertThrows("FX_PROVIDER_UNAVAILABLE", () => service.GetLatestRate("EUR", "USD"));
	provider.IsAvailable = true;
});

Run("fallback to reciprocal provider is supported", () =>
{
	var fallbackProvider = new MutableRateProvider();
	fallbackProvider.SetRate("USD", "EUR", 0.91m);
	var fallbackService = new FxEngineService(
		currencyService,
		fallbackProvider,
		new FxRateCache(TimeSpan.FromMilliseconds(5)),
		new InMemoryFxRateRepository(),
		new InMemoryFxHistoryRepository(),
		new InMemoryFxConversionRepository());
	var rate = fallbackService.GetLatestRate("EUR", "USD");
	Assert(rate.Rate == 1m / 0.91m, "fallback should derive the reciprocal rate");
});

Run("invalid rate period is rejected", () =>
{
	var invalidProvider = new MutableRateProvider();
	invalidProvider.SetInvalidRate("CAD", "EUR", 0.5m);
	var invalidService = new FxEngineService(
		currencyService,
		invalidProvider,
		new FxRateCache(TimeSpan.FromMilliseconds(5)),
		new InMemoryFxRateRepository(),
		new InMemoryFxHistoryRepository(),
		new InMemoryFxConversionRepository());
	try
	{
		invalidService.GetLatestRate("CAD", "EUR");
		throw new Exception("expected FX_RATE_PERIOD_INVALID");
	}
	catch (InvalidOperationException ex) when (ex.Message == "FX_RATE_PERIOD_INVALID")
	{
	}
});

Run("provider replacement changes the resolved rate", () =>
{
	var replacement = new MutableRateProvider();
	replacement.SetRate("CAD", "EUR", 0.5m);
	provider.Inner = replacement;
	var rate = service.GetLatestRate("CAD", "EUR");
	Assert(rate.Rate == 0.5m, "replacement provider should be used for unresolved pairs");
});

Run("creates a quote from a valid rate", () =>
{
	provider.Inner = null;
	provider.SetRate("EUR", "XAF", 655.957m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "XAF", AmountMinor = 10000, Provider = "ECB", SpreadPercentage = 0.0035m, TrustScore = 0.999m });
	Assert(quote.TargetAmountMinor > 0, "quote should contain a positive target amount");
	Assert(quote.Status == QuoteStatus.Created, "new quote should be created");
});

Run("same-currency quote preserves amount", () =>
{
	provider.SetRate("EUR", "EUR", 1m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "EUR", AmountMinor = 12345, Provider = "ECB" });
	Assert(quote.TargetAmountMinor == 12345, "same-currency quote should preserve amount");
});

Run("unknown currency is rejected", () =>
{
	AssertThrows("CURRENCY_NOT_FOUND", () => quoteHandler.Handle(new CreateQuoteRequest { From = "ZZZ", To = "XAF", AmountMinor = 1000 }));
});

Run("absent rate is rejected", () =>
{
	provider.IsAvailable = false;
	AssertThrows("FX_PROVIDER_UNAVAILABLE", () => quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "USD", AmountMinor = 1000 }));
	provider.IsAvailable = true;
});

Run("accepting a quote marks it as accepted", () =>
{
	provider.SetRate("EUR", "XAF", 655.957m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "XAF", AmountMinor = 10000, Provider = "ECB" });
	var acceptor = new AcceptQuoteHandler(quoteRepository);
	var accepted = acceptor.Handle(quote.QuoteId);
	Assert(accepted.Status == QuoteStatus.Accepted, "accepted quote should be marked accepted");
});

Run("consumption is unique", () =>
{
	provider.SetRate("EUR", "XAF", 655.957m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "XAF", AmountMinor = 10000, Provider = "ECB" });
	var acceptor = new AcceptQuoteHandler(quoteRepository);
	acceptor.Handle(quote.QuoteId);
	var quoteEntity = quoteRepository.Get(quote.QuoteId)!;
	quoteEntity.Consume();
	Assert(quoteEntity.Status == QuoteStatus.Consumed, "consumed quote should be marked consumed");
});

Run("spread is applied", () =>
{
	provider.SetRate("EUR", "XAF", 655.957m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "XAF", AmountMinor = 10000, Provider = "ECB", SpreadPercentage = 0.0035m });
	Assert(quote.TargetAmountMinor < 10000 * 655.957m, "spread should reduce the target amount");
});

Run("fee is calculated", () =>
{
	provider.SetRate("EUR", "XAF", 655.957m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "XAF", AmountMinor = 10000, Provider = "ECB" });
	Assert(quote.TotalFeeMinor >= 0, "fee should be non-negative");
});

Run("trust score is propagated", () =>
{
	provider.SetRate("EUR", "XAF", 655.957m);
	var quote = quoteHandler.Handle(new CreateQuoteRequest { From = "EUR", To = "XAF", AmountMinor = 10000, Provider = "ECB", TrustScore = 0.998m });
	Assert(quote.TrustScore == 0.998m, "trust score should be stored on the quote");
});

if (failures.Count == 0)
{
	Console.WriteLine("All AFW-DLV-0004.4 FX scenarios passed.");
	return;
}

Console.WriteLine("AFW-DLV-0004.4 FX scenarios failed:");
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

void AssertThrows(string expectedCode, Action action)
{
	try
	{
		action();
		throw new Exception($"expected {expectedCode}");
	}
	catch (InvalidOperationException ex) when (ex.Message == expectedCode)
	{
	}
}

sealed class MutableRateProvider : IFxRateProvider
{
	private readonly Dictionary<(string BaseCurrency, string QuoteCurrency), decimal> _rates = new();
	private readonly HashSet<(string BaseCurrency, string QuoteCurrency)> _invalidPeriods = new();

	public MutableRateProvider? Inner { get; set; }
	public string Name => Inner?.Name ?? "MutableRateProvider";
	public bool IsAvailable { get; set; } = true;
	public int CallCount { get; private set; }

	public void ResetCallCount() => CallCount = 0;

	public void SetRate(string baseCurrency, string quoteCurrency, decimal rate)
	{
		_rates[(Normalize(baseCurrency), Normalize(quoteCurrency))] = rate;
	}

	public void SetInvalidRate(string baseCurrency, string quoteCurrency, decimal rate)
	{
		_rates[(Normalize(baseCurrency), Normalize(quoteCurrency))] = rate;
		_invalidPeriods.Add((Normalize(baseCurrency), Normalize(quoteCurrency)));
	}

	public Task<ExchangeRate> GetRateAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken)
	{
		if (!IsAvailable)
		{
			throw new InvalidOperationException("FX_PROVIDER_UNAVAILABLE");
		}

		CallCount++;
		if (Inner is not null)
		{
			return Inner.GetRateAsync(baseCurrency, quoteCurrency, cancellationToken);
		}

		var key = (Normalize(baseCurrency), Normalize(quoteCurrency));
		if (!_rates.TryGetValue(key, out var rate))
		{
			throw new InvalidOperationException("FX_RATE_NOT_FOUND");
		}

		var now = DateTimeOffset.UtcNow;
		var validFrom = now;
		var validUntil = now.AddMinutes(10);
		if (_invalidPeriods.Contains(key))
		{
			validFrom = now.AddMinutes(10);
			validUntil = now;
		}
		return Task.FromResult(new ExchangeRate(
			Guid.CreateVersion7(),
			key.Item1,
			key.Item2,
			rate,
			Name,
			null,
			validFrom,
			validUntil,
			ExchangeRateStatus.Active,
			1,
			now));
	}

	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(string baseCurrency, CancellationToken cancellationToken)
	{
		return Task.FromResult<IReadOnlyList<ExchangeRate>>([]);
	}

	private static string Normalize(string currency) => currency.Trim().ToUpperInvariant();
}
